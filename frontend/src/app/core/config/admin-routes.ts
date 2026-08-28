/** Non-obvious frontend path for the admin area (JWT still required). */
export const ADMIN_BASE_PATH = 'manage-nz7k4p';

export function adminUrl(...segments: string[]): string {
  const rest = segments.filter(Boolean).join('/');
  return rest ? `/${ADMIN_BASE_PATH}/${rest}` : `/${ADMIN_BASE_PATH}`;
}
