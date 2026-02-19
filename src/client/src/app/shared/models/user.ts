export interface IUser {
    email: string;
    displayName: string;
    token: string;
}

export interface IExternalLoginInfo {
    provider: string;
    providerDisplayName: string;
    isLinked: boolean;
}

export interface IUserWithExternalLogins {
    hasPassword: boolean;
    externalLogins: IExternalLoginInfo[];
    email: string;
    displayName: string;
}
