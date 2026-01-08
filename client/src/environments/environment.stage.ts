// This file is a TEMPLATE - tokens replaced by Azure DevOps before build
export const environment = {
  production: '#{isProd}#',
  apiUrl: '#{apiUrl}#',
  stripeSettings: {
    publishableKey: '#{stripeKey}#'
  }
};