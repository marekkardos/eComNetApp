// This file can be replaced during build by using the `fileReplacements` array.
// `ng build --prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

export const  environment = {
  production: false,
  apiUrl: 'http://localhost:44369/api/',
  stripeSettings: {
    PublishableKey: 'pk_test_51I5A6QL9LzbX0TGfv5CuYr7xuET3rORDb3jbHIFD2QfSYK2le5EyhhMavjVvJx3DM12eg2b1RR8DjmfM4nw6Mcyl00jRYJIO8a'
  }
};

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/dist/zone-error';  // Included with Angular CLI.
