// --- Diagnostic event handlers (remove after debugging) ---
                options.Events = new OAuthEvents
                {
                    OnRedirectToAuthorizationEndpoint = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("GoogleOAuth.Diagnostics");

                        logger.LogWarning("[DIAG] Challenge redirect. Scheme: {Scheme}, Host: {Host}",
                            context.HttpContext.Request.Scheme,
                            context.HttpContext.Request.Host);
                        logger.LogWarning("[DIAG] RedirectUri: {Uri}", context.RedirectUri);

                        // Log Set-Cookie headers that will be sent
                        foreach (var header in context.Response.Headers)
                        {
                            if (header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
                            {
                                logger.LogWarning("[DIAG] Set-Cookie: {Value}", header.Value);
                            }
                        }

                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    },
                    OnRemoteFailure = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("GoogleOAuth.Diagnostics");

                        var stateParam = context.Request.Query["state"].ToString();
                        logger.LogError("[DIAG] RemoteFailure: {Error}", context.Failure?.Message);
                        logger.LogError("[DIAG] Inner: {Inner}", context.Failure?.InnerException?.Message);
                        logger.LogError("[DIAG] Callback Scheme: {Scheme}, Host: {Host}",
                            context.Request.Scheme, context.Request.Host);
                        logger.LogError("[DIAG] State param length: {Len}, first 80 chars: {State}",
                            stateParam.Length,
                            stateParam.Length > 80 ? stateParam[..80] : stateParam);

                        // Log all cookies present on the callback request
                        logger.LogError("[DIAG] Cookies on callback ({Count}):", context.Request.Cookies.Count);
                        foreach (var cookie in context.Request.Cookies)
                        {
                            logger.LogError("[DIAG]   Cookie: {Name} (value length: {Len})",
                                cookie.Key, cookie.Value.Length);
                        }

                        // Reproduce SecureDataFormat.Unprotect step-by-step
                        try
                        {
                            // Step 1: Base64Url decode
                            var protectedBytes = WebEncoders.Base64UrlDecode(stateParam);
                            logger.LogError("[DIAG] Step1 Base64Url decode OK, {Len} bytes", protectedBytes.Length);

                            // Step 2: Data Protection decrypt (same purpose chain as OAuthPostConfigureOptions)
                            var dp = context.HttpContext.RequestServices
                                .GetRequiredService<IDataProtectionProvider>();
                            var protector = dp.CreateProtector(
                                "Microsoft.AspNetCore.Authentication.Google.GoogleHandler",
                                "Google", "v1");
                            try
                            {
                                var decryptedBytes = protector.Unprotect(protectedBytes);
                                logger.LogError("[DIAG] Step2 DataProtection.Unprotect OK, {Len} bytes",
                                    decryptedBytes.Length);

                                // Step 3: Deserialize AuthenticationProperties
                                try
                                {
                                    var serializer = new Microsoft.AspNetCore.Authentication
                                        .PropertiesSerializer();
                                    var props = serializer.Deserialize(decryptedBytes);
                                    logger.LogError("[DIAG] Step3 Deserialize OK. RedirectUri: {Uri}",
                                        props?.RedirectUri);
                                }
                                catch (Exception ex3)
                                {
                                    logger.LogError(ex3, "[DIAG] Step3 Deserialize FAILED");
                                }
                            }
                            catch (Exception ex2)
                            {
                                logger.LogError(ex2, "[DIAG] Step2 DataProtection.Unprotect FAILED");
                            }
                        }
                        catch (Exception ex1)
                        {
                            logger.LogError(ex1, "[DIAG] Step1 Base64Url decode FAILED");
                        }

                        return Task.CompletedTask;
                    }
                };