"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  createBranchAction,
  createRoleAction,
  createUserAction,
  resetUserPasswordAction,
  setBranchStatusAction,
  setUserStatusAction,
  updateBranchAction,
  updateRoleAction,
  updateUserAction,
  type BranchFormPayload,
  type RoleFormPayload,
  type UserFormPayload,
} from "@/app/settings/actions";
import { DetailList, EmptyState, RecordCard, RecordGrid, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { formatDateTime } from "@/lib/format";
import type { ActionResult } from "@/lib/action-result";
import type { AuthUser } from "@/types/auth";
import type { Branch, PermissionCatalog, Role, User } from "@/types/settings";
import type { SystemHealth } from "@/types/system";

const roleTypeOptions = [
  { value: "1", label: "Admin" },
  { value: "2", label: "Manager" },
  { value: "3", label: "Staff" },
];

const emptyBranch: BranchFormPayload = { name: "", city: "", address: "", phone: "" };
const emptyRole: RoleFormPayload = { name: "", roleType: "3", permissions: [] };
const emptyUser: UserFormPayload = { roleId: "", branchId: "", fullName: "", email: "", password: "" };

export function SettingsWorkspace({
  profile,
  users,
  roles,
  branches,
  permissions,
  system,
}: {
  profile: AuthUser;
  users: User[];
  roles: Role[];
  branches: Branch[];
  permissions: PermissionCatalog[];
  system: SystemHealth;
}) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [notice, setNotice] = useState<ActionResult | null>(null);

  const [branchOpen, setBranchOpen] = useState(false);
  const [roleOpen, setRoleOpen] = useState(false);
  const [userOpen, setUserOpen] = useState(false);

  const [branchMode, setBranchMode] = useState<"create" | "edit">("create");
  const [roleMode, setRoleMode] = useState<"create" | "edit">("create");
  const [userMode, setUserMode] = useState<"create" | "edit">("create");

  const [editingBranchId, setEditingBranchId] = useState("");
  const [editingRoleId, setEditingRoleId] = useState("");
  const [editingUserId, setEditingUserId] = useState("");

  const [branchForm, setBranchForm] = useState<BranchFormPayload>(emptyBranch);
  const [roleForm, setRoleForm] = useState<RoleFormPayload>(emptyRole);
  const [userForm, setUserForm] = useState<UserFormPayload>({
    ...emptyUser,
    roleId: roles[0]?.id ?? "",
    branchId: branches[0]?.id ?? "",
  });

  function updateNotice(result: ActionResult) {
    setNotice(result);
    if (result.ok) {
      router.refresh();
    }
  }

  function submitBranch() {
    startTransition(async () => {
      const result =
        branchMode === "create"
          ? await createBranchAction(branchForm)
          : await updateBranchAction(editingBranchId, branchForm);
      updateNotice(result);
      if (result.ok) {
        setBranchOpen(false);
      }
    });
  }

  function submitRole() {
    startTransition(async () => {
      const result = roleMode === "create" ? await createRoleAction(roleForm) : await updateRoleAction(editingRoleId, roleForm);
      updateNotice(result);
      if (result.ok) {
        setRoleOpen(false);
      }
    });
  }

  function submitUser() {
    startTransition(async () => {
      const result = userMode === "create" ? await createUserAction(userForm) : await updateUserAction(editingUserId, userForm);
      updateNotice(result);
      if (result.ok) {
        setUserOpen(false);
      }
    });
  }

  function openBranchCreate() {
    setBranchMode("create");
    setBranchForm(emptyBranch);
    setBranchOpen(true);
  }

  function openRoleCreate() {
    setRoleMode("create");
    setRoleForm(emptyRole);
    setRoleOpen(true);
  }

  function openUserCreate() {
    setUserMode("create");
    setUserForm({ ...emptyUser, roleId: roles[0]?.id ?? "", branchId: branches[0]?.id ?? "" });
    setUserOpen(true);
  }

  return (
    <div className="space-y-6">
      {notice ? <div className={`alert-banner ${notice.ok ? "alert-banner--success" : "alert-banner--error"}`}>{notice.message}</div> : null}

      <StatGrid>
        <StatCard label="Current user" value={profile.fullName} note={profile.email} />
        <StatCard label="Active users" value={String(users.filter((user) => user.isActive).length)} note={`${users.length} total`} tone="accent" />
        <StatCard label="Roles" value={String(roles.length)} />
        <StatCard label="Branches" value={String(branches.length)} note={`${branches.filter((branch) => branch.isActive).length} active`} />
        <StatCard label="System status" value={system.status} note={system.environment} tone="warm" />
      </StatGrid>

      <Surface title="User access directory" description="Create, edit, deactivate, and reset credentials for user accounts.">
        <div className="flex flex-wrap gap-3">
          <Button onClick={openUserCreate}>Create user</Button>
        </div>
        {users.length === 0 ? (
          <EmptyState message="No users are available from the backend yet." />
        ) : (
          <RecordGrid>
            {users.map((user) => (
              <RecordCard key={user.id} title={user.fullName} subtitle={user.email}>
                <DetailList
                  items={[
                    { label: "Role", value: `${user.roleName} · ${String(user.roleType)}` },
                    { label: "Branch", value: user.branchName },
                    { label: "Status", value: user.isActive ? "Active" : "Inactive" },
                    { label: "Created", value: formatDateTime(user.createdAtUtc) },
                  ]}
                />
                <div className="mt-4 flex flex-wrap gap-3">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setUserMode("edit");
                      setEditingUserId(user.id);
                      setUserForm({
                        roleId: user.roleId,
                        branchId: user.branchId,
                        fullName: user.fullName,
                        email: user.email,
                        password: "",
                      });
                      setUserOpen(true);
                    }}
                  >
                    Edit
                  </Button>
                  <Button variant="outline" onClick={() => startTransition(async () => updateNotice(await setUserStatusAction(user.id, !user.isActive)))} disabled={pending}>
                    {user.isActive ? "Deactivate" : "Activate"}
                  </Button>
                  <Button variant="outline" onClick={() => startTransition(async () => updateNotice(await resetUserPasswordAction(user.id, "change-me-now")))} disabled={pending}>
                    Reset password
                  </Button>
                </div>
              </RecordCard>
            ))}
          </RecordGrid>
        )}
      </Surface>

      <Surface title="Branch directory" description="Create new branches, edit branch metadata, and change branch status.">
        <div className="flex flex-wrap gap-3">
          <Button onClick={openBranchCreate}>Create branch</Button>
        </div>
        {branches.length === 0 ? (
          <EmptyState message="No branches are available from the backend yet." />
        ) : (
          <RecordGrid>
            {branches.map((branch) => (
              <RecordCard key={branch.id} title={branch.name} subtitle={branch.city}>
                <DetailList
                  items={[
                    { label: "Address", value: branch.address },
                    { label: "Phone", value: branch.phone },
                    { label: "Status", value: branch.isActive ? "Active" : "Inactive" },
                  ]}
                />
                <div className="mt-4 flex flex-wrap gap-3">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setBranchMode("edit");
                      setEditingBranchId(branch.id);
                      setBranchForm({
                        name: branch.name,
                        city: branch.city,
                        address: branch.address,
                        phone: branch.phone,
                      });
                      setBranchOpen(true);
                    }}
                  >
                    Edit
                  </Button>
                  <Button variant="outline" onClick={() => startTransition(async () => updateNotice(await setBranchStatusAction(branch.id, !branch.isActive)))} disabled={pending}>
                    {branch.isActive ? "Deactivate" : "Activate"}
                  </Button>
                </div>
              </RecordCard>
            ))}
          </RecordGrid>
        )}
      </Surface>

      <Surface title="Role catalog" description="Manage roles and permission bundles in the admin workspace.">
        <div className="flex flex-wrap gap-3">
          <Button onClick={openRoleCreate}>Create role</Button>
        </div>
        {roles.length === 0 ? (
          <EmptyState message="No roles are available from the backend yet." />
        ) : (
          <RecordGrid>
            {roles.map((role) => (
              <RecordCard key={role.id} title={role.name} subtitle={String(role.roleType)}>
                <DetailList
                  items={[
                    { label: "Created", value: formatDateTime(role.createdAtUtc) },
                    { label: "Permissions JSON", value: role.permissionsJson },
                  ]}
                />
                <div className="mt-4 flex flex-wrap gap-3">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setRoleMode("edit");
                      setEditingRoleId(role.id);
                      setRoleForm({
                        name: role.name,
                        roleType: String(role.roleType),
                        permissions: parsePermissions(role.permissionsJson),
                      });
                      setRoleOpen(true);
                    }}
                  >
                    Edit
                  </Button>
                </div>
              </RecordCard>
            ))}
          </RecordGrid>
        )}
      </Surface>

      <BranchDialog open={branchOpen} mode={branchMode} form={branchForm} onOpenChange={setBranchOpen} onChange={setBranchForm} onSubmit={submitBranch} pending={pending} />
      <RoleDialog open={roleOpen} mode={roleMode} form={roleForm} permissions={permissions} onOpenChange={setRoleOpen} onChange={setRoleForm} onSubmit={submitRole} pending={pending} />
      <UserDialog open={userOpen} mode={userMode} form={userForm} roles={roles} branches={branches} onOpenChange={setUserOpen} onChange={setUserForm} onSubmit={submitUser} pending={pending} />
    </div>
  );
}

function parsePermissions(value: string) {
  try {
    const parsed = JSON.parse(value);
    return Array.isArray(parsed) ? parsed.filter((item): item is string => typeof item === "string") : [];
  } catch {
    return [];
  }
}

function BranchDialog({
  open,
  mode,
  form,
  onOpenChange,
  onChange,
  onSubmit,
  pending,
}: {
  open: boolean;
  mode: "create" | "edit";
  form: BranchFormPayload;
  onOpenChange: (open: boolean) => void;
  onChange: (form: BranchFormPayload) => void;
  onSubmit: () => void;
  pending: boolean;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{mode === "create" ? "Create branch" : "Edit branch"}</DialogTitle>
        </DialogHeader>
        <div className="dialog-form-grid">
          <Field label="Name" value={form.name} onChange={(value) => onChange({ ...form, name: value })} />
          <Field label="City" value={form.city} onChange={(value) => onChange({ ...form, city: value })} />
          <Field label="Address" value={form.address} onChange={(value) => onChange({ ...form, address: value })} />
          <Field label="Phone" value={form.phone} onChange={(value) => onChange({ ...form, phone: value })} />
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
          <Button onClick={onSubmit} disabled={pending}>Save</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function RoleDialog({
  open,
  mode,
  form,
  permissions,
  onOpenChange,
  onChange,
  onSubmit,
  pending,
}: {
  open: boolean;
  mode: "create" | "edit";
  form: RoleFormPayload;
  permissions: PermissionCatalog[];
  onOpenChange: (open: boolean) => void;
  onChange: (form: RoleFormPayload) => void;
  onSubmit: () => void;
  pending: boolean;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{mode === "create" ? "Create role" : "Edit role"}</DialogTitle>
        </DialogHeader>
        <div className="grid gap-4">
          <Field label="Name" value={form.name} onChange={(value) => onChange({ ...form, name: value })} />
          <SelectField label="Role type" value={form.roleType} onValueChange={(value) => onChange({ ...form, roleType: value })} options={roleTypeOptions} />
          <div className="form-field form-field--full gap-3">
            <Label>Permissions</Label>
            <div className="grid gap-3 rounded-lg border p-4 md:grid-cols-2">
              {permissions.map((permission) => (
                <label key={permission.key} className="flex items-start gap-3 text-sm">
                  <Checkbox
                    checked={form.permissions.includes(permission.key)}
                    onCheckedChange={(checked) =>
                      onChange({
                        ...form,
                        permissions: checked
                          ? [...form.permissions, permission.key]
                          : form.permissions.filter((item) => item !== permission.key),
                      })
                    }
                  />
                  <span>{permission.label}</span>
                </label>
              ))}
            </div>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
          <Button onClick={onSubmit} disabled={pending}>Save</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function UserDialog({
  open,
  mode,
  form,
  roles,
  branches,
  onOpenChange,
  onChange,
  onSubmit,
  pending,
}: {
  open: boolean;
  mode: "create" | "edit";
  form: UserFormPayload;
  roles: Role[];
  branches: Branch[];
  onOpenChange: (open: boolean) => void;
  onChange: (form: UserFormPayload) => void;
  onSubmit: () => void;
  pending: boolean;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{mode === "create" ? "Create user" : "Edit user"}</DialogTitle>
        </DialogHeader>
        <div className="dialog-form-grid">
          <SelectField label="Role" value={form.roleId} onValueChange={(value) => onChange({ ...form, roleId: value })} options={roles.map((role) => ({ value: role.id, label: role.name }))} />
          <SelectField label="Branch" value={form.branchId} onValueChange={(value) => onChange({ ...form, branchId: value })} options={branches.map((branch) => ({ value: branch.id, label: branch.name }))} />
          <Field label="Full name" value={form.fullName} onChange={(value) => onChange({ ...form, fullName: value })} />
          <Field label="Email" type="email" value={form.email} onChange={(value) => onChange({ ...form, email: value })} />
          {mode === "create" ? <Field label="Password" type="password" value={form.password} onChange={(value) => onChange({ ...form, password: value })} /> : null}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Cancel</Button>
          <Button onClick={onSubmit} disabled={pending}>Save</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function Field({ label, value, onChange, type = "text" }: { label: string; value: string; onChange: (value: string) => void; type?: string }) {
  return (
    <div className="form-field">
      <Label>{label}</Label>
      <Input type={type} value={value} onChange={(event) => onChange(event.target.value)} />
    </div>
  );
}

function SelectField({
  label,
  value,
  onValueChange,
  options,
}: {
  label: string;
  value: string;
  onValueChange: (value: string) => void;
  options: Array<{ value: string; label: string }>;
}) {
  return (
    <div className="form-field">
      <Label>{label}</Label>
      <Select value={value} onValueChange={onValueChange}>
        <SelectTrigger>
          <SelectValue placeholder={`Select ${label.toLowerCase()}`} />
        </SelectTrigger>
        <SelectContent>
          {options.map((option) => (
            <SelectItem key={option.value} value={option.value}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}
