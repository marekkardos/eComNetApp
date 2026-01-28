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

export interface IUserWithExternalLogins extends IUser {
    hasPassword: boolean;
    externalLogins: IExternalLoginInfo[];
}
