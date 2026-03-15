export type User = {
  id: string;
  roleId: string;
  roleName: string;
  roleType: string | number;
  branchId: string;
  branchName: string;
  fullName: string;
  email: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};

export type Role = {
  id: string;
  name: string;
  roleType: string | number;
  permissionsJson: string;
  createdAtUtc: string;
};

export type PermissionCatalog = {
  key: string;
  label: string;
};

export type Branch = {
  id: string;
  name: string;
  city: string;
  address: string;
  phone: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
};
