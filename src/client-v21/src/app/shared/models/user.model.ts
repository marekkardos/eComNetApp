export interface User {
  email: string;
  displayName: string;
  token: string;
}

export interface Address {
  firstName: string;
  lastName: string;
  street: string;
  city: string;
  state: string;
  zipcode: string;
  country?: string;
}

export interface ExternalLoginInfo {
  provider: string;
  providerDisplayName: string;
  isLinked: boolean;
}

export interface UserWithExternalLogins extends User {
  externalLogins: ExternalLoginInfo[];
  hasPassword: boolean;
}
