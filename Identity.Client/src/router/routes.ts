export const RouteNames = {
  Root: 'Root',
  Login: 'Login',
  Logout: 'Logout',
  Error: 'Error',
  NotFound: 'NotFound',
  Account: 'Account',
  AccountDefault: 'AccountDefault',
  AccountInfo: 'AccountInfo',
  Security: 'Security',
  AdminUsers: 'AdminUsers',
  AdminRoles: 'AdminRoles',
  AdminApplications: 'AdminApplications',
} as const

export type RouteName = (typeof RouteNames)[keyof typeof RouteNames]
