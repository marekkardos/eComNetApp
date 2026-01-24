export interface IExternalLoginInfo {
    loginProvider: string;
    providerKey: string;
    providerDisplayName: string;
}

export interface IExternalLoginRequest {
    provider: string;
    returnUrl?: string;
}

export interface ILinkExternalLoginRequest {
    provider: string;
    returnUrl?: string;
}

export interface IExternalLoginProvider {
    name: string;
    displayName: string;
    iconClass?: string;
}

export const EXTERNAL_LOGIN_PROVIDERS: IExternalLoginProvider[] = [
    { name: 'Google', displayName: 'Google', iconClass: 'fab fa-google' }
];
