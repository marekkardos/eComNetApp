#!/usr/bin/env node
/**
 * Cross-platform SonarQube analyzer + issue fetcher
 * 
 * Run from project directory:
 *   cd src/API && node ../sonar-fetch.mjs
 *   cd src/client && node ../sonar-fetch.mjs
 * 
 * This script:
 *   1. Detects project type (.NET or Node/Angular)
 *   2. Runs appropriate SonarQube scanner
 *   3. Waits for analysis to complete
 *   4. Fetches issues to sonar-issues.md
 * 
 * Use --fetch-only to skip analysis and just fetch existing issues
 */

import https from 'https';
import http from 'http';
import { writeFileSync, readFileSync, existsSync } from 'fs';
import { resolve } from 'path';
import { execSync, spawn } from 'child_process';

const cwd = process.cwd();

const config = {
  sonarUrl: process.env.SONAR_URL || 'http://localhost:9000',
  sonarToken: process.env.SONAR_TOKEN || '',
  projectKey: process.env.SONAR_PROJECT || '',
  outputFile: process.env.SONAR_OUTPUT || 'sonar-issues.md',
  severities: process.env.SONAR_SEVERITIES || 'BLOCKER,CRITICAL,MAJOR',
  maxIssues: parseInt(process.env.SONAR_MAX_ISSUES || '100', 10),
  fetchOnly: false,
  skipBuild: false,
};

const args = process.argv.slice(2);
for (let i = 0; i < args.length; i++) {
  switch (args[i]) {
    case '--url': config.sonarUrl = args[++i]; break;
    case '--token': config.sonarToken = args[++i]; break;
    case '--project': config.projectKey = args[++i]; break;
    case '--output': config.outputFile = args[++i]; break;
    case '--severities': config.severities = args[++i]; break;
    case '--max': config.maxIssues = parseInt(args[++i], 10); break;
    case '--fetch-only': config.fetchOnly = true; break;
    case '--skip-build': config.skipBuild = true; break;
    case '--help':
      console.log(`
SonarQube Analyzer + Issue Fetcher

Usage (from project directory):
  cd src/API && node ../sonar-fetch.mjs
  cd src/client && node ../sonar-fetch.mjs

Options:
  --url <url>          SonarQube server URL (default: http://localhost:9000)
  --token <token>      Authentication token
  --project <key>      Project key (overrides .sonarqube.json)
  --output <file>      Output file (default: sonar-issues.md)
  --severities <list>  Comma-separated (default: BLOCKER,CRITICAL,MAJOR)
  --max <number>       Maximum issues (default: 100)
  --fetch-only         Skip analysis, just fetch existing issues
  --skip-build         Skip build step (for .NET, assumes already built)

Config: Reads .sonarqube.json from current directory
      `);
      process.exit(0);
  }
}

// Load config from current directory
const configPath = resolve(cwd, '.sonarqube.json');
if (existsSync(configPath)) {
  try {
    const fileConfig = JSON.parse(readFileSync(configPath, 'utf-8'));
    if (!config.sonarUrl || config.sonarUrl === 'http://localhost:9000') {
      config.sonarUrl = fileConfig.url || config.sonarUrl;
    }
    if (!config.sonarToken) config.sonarToken = fileConfig.token || '';
    if (!config.projectKey) config.projectKey = fileConfig.projectKey || '';
    console.log(`Loaded config from: ${configPath}`);
  } catch (e) {
    console.error(`Warning: Could not parse ${configPath}`);
  }
}

if (!config.projectKey) {
  console.error('Error: Project key required.');
  console.error('Create .sonarqube.json or use --project flag');
  process.exit(1);
}

if (!config.sonarToken) {
  console.error('Error: SonarQube token required.');
  console.error('Add token to .sonarqube.json or use --token flag');
  process.exit(1);
}

const outputPath = resolve(cwd, config.outputFile);

// ============ Project Type Detection ============

function detectProjectType() {
  if (existsSync(resolve(cwd, '*.csproj')) || 
      existsSync(resolve(cwd, '*.sln')) ||
      execSync('dir /b *.csproj *.sln 2>nul || ls *.csproj *.sln 2>/dev/null || echo ""', { cwd, encoding: 'utf-8' }).trim()) {
    // Check more carefully for .NET
    try {
      const files = execSync('dir /b 2>nul || ls', { cwd, encoding: 'utf-8', shell: true });
      if (files.includes('.csproj') || files.includes('.sln')) {
        return 'dotnet';
      }
    } catch {}
  }
  
  if (existsSync(resolve(cwd, 'package.json'))) {
    return 'node';
  }
  
  if (existsSync(resolve(cwd, 'sonar-project.properties'))) {
    return 'generic';
  }
  
  return 'unknown';
}

// ============ Run Commands ============

function runCommand(command, description, checkOutput = null) {
  console.log(`\n> ${description}`);
  console.log(`$ ${command}\n`);
  try {
    const output = execSync(command, { cwd, encoding: 'utf-8', shell: true, stdio: ['inherit', 'pipe', 'pipe'] });
    console.log(output);
    
    if (checkOutput) {
      return output.includes(checkOutput);
    }
    return true;
  } catch (error) {
    // execSync throws on non-zero exit, but output might still have what we need
    if (error.stdout) console.log(error.stdout);
    if (error.stderr) console.error(error.stderr);
    
    if (checkOutput && error.stdout?.includes(checkOutput)) {
      return true;
    }
    
    console.error(`Failed: ${description}`);
    return false;
  }
}

function runCommandInherit(command, description) {
  console.log(`\n> ${description}`);
  console.log(`$ ${command}\n`);
  try {
    execSync(command, { cwd, stdio: 'inherit', shell: true });
    return true;
  } catch (error) {
    console.error(`Failed: ${description}`);
    return false;
  }
}

// ============ .NET Analysis ============

async function runDotNetAnalysis() {
  console.log('\n=== Running .NET SonarQube Analysis ===\n');
  
  const beginCmd = `dotnet sonarscanner begin /k:"${config.projectKey}" /d:sonar.host.url="${config.sonarUrl}" /d:sonar.token="${config.sonarToken}"`;
  
  if (!runCommandInherit(beginCmd, 'SonarScanner Begin')) {
    console.error('\nMake sure dotnet-sonarscanner is installed:');
    console.error('  dotnet tool install --global dotnet-sonarscanner');
    return { success: false };
  }
  
  if (!config.skipBuild) {
    if (!runCommandInherit('dotnet build', 'Build Project')) {
      return { success: false };
    }
  }
  
  const endCmd = `dotnet sonarscanner end /d:sonar.token="${config.sonarToken}"`;
  const postProcessingOk = runCommand(endCmd, 'SonarScanner End', 'Post-processing succeeded');
  
  if (!postProcessingOk) {
    return { success: false };
  }
  
  // Post-processing succeeded means analysis is uploaded and processing
  return { success: true, skipWait: true };
}

// ============ Node/Angular Analysis ============

async function runNodeAnalysis() {
  console.log('\n=== Running Node/Angular SonarQube Analysis ===\n');
  
  // Check for sonar-scanner
  const scannerCmd = `sonar-scanner -Dsonar.projectKey=${config.projectKey} -Dsonar.host.url=${config.sonarUrl} -Dsonar.token=${config.sonarToken}`;
  
  // Add sources if typical Angular structure
  let sources = 'src';
  if (existsSync(resolve(cwd, 'src/app'))) {
    sources = 'src';
  }
  
  const fullCmd = `${scannerCmd} -Dsonar.sources=${sources}`;
  
  // Check for "EXECUTION SUCCESS" in output
  const success = runCommand(fullCmd, 'SonarScanner', 'EXECUTION SUCCESS');
  
  if (!success) {
    console.error('\nMake sure sonar-scanner is installed:');
    console.error('  npm install -g sonarqube-scanner');
    console.error('  # or download from https://docs.sonarqube.org/latest/analysis/scan/sonarscanner/');
    return { success: false };
  }
  
  return { success: true, skipWait: false };
}

// ============ HTTP Helpers ============

function httpRequest(url) {
  return new Promise((resolve, reject) => {
    const parsedUrl = new URL(url);
    const client = parsedUrl.protocol === 'https:' ? https : http;
    
    const req = client.request(parsedUrl, {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
        'Authorization': `Basic ${Buffer.from(config.sonarToken + ':').toString('base64')}`,
      },
    }, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        if (res.statusCode >= 200 && res.statusCode < 300) {
          try { resolve(JSON.parse(data)); } 
          catch { resolve(data); }
        } else {
          reject(new Error(`HTTP ${res.statusCode}: ${data}`));
        }
      });
    });
    req.on('error', reject);
    req.end();
  });
}

// ============ Wait for Analysis ============

async function waitForAnalysis(maxWaitSeconds = 120) {
  console.log('\nWaiting for SonarQube to process analysis...');
  
  const baseUrl = config.sonarUrl.replace(/\/$/, '');
  const startTime = Date.now();
  
  while ((Date.now() - startTime) < maxWaitSeconds * 1000) {
    try {
      const result = await httpRequest(
        `${baseUrl}/api/ce/component?component=${encodeURIComponent(config.projectKey)}`
      );
      
      if (result.current) {
        const status = result.current.status;
        if (status === 'SUCCESS') {
          console.log('Analysis complete!');
          return true;
        } else if (status === 'FAILED' || status === 'CANCELED') {
          console.error(`Analysis ${status.toLowerCase()}`);
          return false;
        }
        process.stdout.write('.');
      } else if (result.queue && result.queue.length === 0) {
        // No pending tasks, might be ready
        console.log('Analysis complete!');
        return true;
      }
    } catch (e) {
      // Might not be ready yet, continue waiting
    }
    
    await new Promise(r => setTimeout(r, 3000));
  }
  
  console.log('\nTimeout waiting for analysis. Fetching available issues anyway...');
  return true;
}

// ============ Fetch Issues ============

async function fetchIssues() {
  const baseUrl = config.sonarUrl.replace(/\/$/, '');
  const params = new URLSearchParams({
    componentKeys: config.projectKey,
    resolved: 'false',
    severities: config.severities,
    ps: config.maxIssues.toString(),
  });

  console.log(`\nFetching issues from ${baseUrl}...`);
  
  try {
    return await httpRequest(`${baseUrl}/api/issues/search?${params}`);
  } catch (error) {
    console.error(`Failed to fetch issues: ${error.message}`);
    process.exit(1);
  }
}

function formatAsMarkdown(data) {
  const lines = [
    '# SonarQube Analysis Results',
    '',
    `**Project:** ${config.projectKey}`,
    `**Server:** ${config.sonarUrl}`,
    `**Total Issues:** ${data.total || 0}`,
    `**Generated:** ${new Date().toISOString()}`,
    '',
  ];

  if (!data.issues?.length) {
    lines.push('No issues found matching criteria.');
    return lines.join('\n');
  }

  const bySeverity = {};
  for (const issue of data.issues) {
    const sev = issue.severity || 'UNKNOWN';
    if (!bySeverity[sev]) bySeverity[sev] = [];
    bySeverity[sev].push(issue);
  }

  for (const severity of ['BLOCKER', 'CRITICAL', 'MAJOR', 'MINOR', 'INFO']) {
    const issues = bySeverity[severity];
    if (!issues?.length) continue;

    lines.push(`## ${severity} (${issues.length})`, '');

    for (const issue of issues) {
      const file = issue.component?.replace(`${config.projectKey}:`, '') || 'unknown';
      lines.push(
        `### ${issue.message}`,
        '',
        `- **File:** \`${file}\``,
        `- **Line:** ${issue.line || '?'}`,
        `- **Rule:** ${issue.rule}`,
        `- **Type:** ${issue.type}`,
        ''
      );
    }
  }

  return lines.join('\n');
}

// ============ Main ============

async function main() {
  let skipWait = false;
  
  if (!config.fetchOnly) {
    const projectType = detectProjectType();
    console.log(`Detected project type: ${projectType}`);
    
    let result = { success: false, skipWait: false };
    
    switch (projectType) {
      case 'dotnet':
        result = await runDotNetAnalysis();
        break;
      case 'node':
        result = await runNodeAnalysis();
        break;
      case 'generic':
        console.log('Using sonar-project.properties...');
        const ok = runCommandInherit('sonar-scanner', 'SonarScanner');
        result = { success: ok, skipWait: false };
        break;
      default:
        console.error('Could not detect project type.');
        console.error('Use --fetch-only to skip analysis, or create sonar-project.properties');
        process.exit(1);
    }
    
    if (!result.success) {
      console.error('\nAnalysis failed. Use --fetch-only to fetch existing issues.');
      process.exit(1);
    }
    
    skipWait = result.skipWait;
    
    if (!skipWait) {
      await waitForAnalysis();
    } else {
      console.log('\nPost-processing succeeded, waiting briefly for server...');
      await new Promise(r => setTimeout(r, 5000)); // Brief wait for server to index
    }
  } else {
    console.log('Skipping analysis (--fetch-only mode)');
  }
  
  const data = await fetchIssues();
  const markdown = formatAsMarkdown(data);
  
  writeFileSync(outputPath, markdown, 'utf-8');
  console.log(`\nIssues written to: ${outputPath}`);
  console.log(`Total: ${data.total}, Fetched: ${data.issues?.length || 0}`);
}

main().catch(console.error);
