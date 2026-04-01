# GymManagement Architecture

## Domain scope

`GymManagement` handles business rules for:
- Platform subscription tiers (ShapeUp plans)
- User role composition (same user can be `Trainer`, `IndependentClient`, `GymOwner` simultaneously)
- Internal relationship roles: `Client` (client linked to independent trainer) and `GymClient` (client linked to gym)
- Gym operation model (owner, receptionist, trainer, client)
- Gym-owned plans and trainer-owned plans
- Client assignment rules inside gyms and for independent trainers

## Database structure

Context: `Features/GymManagement/Infrastructure/Data/GymManagementDbContext.cs`

Main tables:
- `PlatformTiers`
  - Platform plans, dynamic CRUD
  - **`TargetRole` (required)** — defines the audience of the tier: `IndependentClient`, `Trainer`, or `GymOwner`
  - A tier cannot be assigned to a `UserPlatformRole` whose `Role` differs from the tier's `TargetRole` (validated at `AssignUserRoleHandler`)
- `UserPlatformRoles`
  - N-role model per user (`UserId + Role` unique)
  - Public assignable roles: `Trainer`, `IndependentClient`, `GymOwner`
  - Internal managed roles: `Client`, `GymClient` (never assigned via `POST /user-roles`)
  - Optional `PlatformTierId` — when provided, its `TargetRole` must match the assigned `Role`
- `Gyms`
  - Gym identity, owner (`OwnerId`), optional platform tier
- `GymPlans`
  - Plans owned by a gym (`GymId`)
- `GymStaff`
  - Users linked to gym as `Trainer` or `Receptionist`
- `GymClients`
  - Gym client enrollment, linked to one gym plan and optional trainer
- `TrainerPlans`
  - Independent trainer plans (owned by `TrainerId`)
- `TrainerClients`
  - Independent trainer client assignments and transfer support
  - `TrainerPlanId` is optional at relationship creation time (plan can be assigned later)
  - One active independent-trainer link per client (`ClientId` uniqueness enforced by business rule)
  - A user cannot be in `TrainerClients` and `GymClients` simultaneously
- `TrainerClientInvites`
  - Email-based invitation links for trainer-client onboarding
  - Stores `InviteeEmail`, single-use `AccessTokenHash`, optional `TrainerPlanId`, expiration and status
  - Invite email uses template `46dbcd80-c134-407d-ad36-2fe01ed0ca89`
  - `register_url` contains a Base64Url-obfuscated payload with `trainerId` and invite token
  - Accept endpoint receives that encoded payload and decodes it server-side before materializing the `TrainerClients` row

## Endpoints

### Platform tiers
- `GET /api/gym-management/platform-tiers`
- `POST /api/gym-management/platform-tiers`
- `PUT /api/gym-management/platform-tiers/{id}`
- `DELETE /api/gym-management/platform-tiers/{id}`

### User roles
- `GET /api/gym-management/user-roles/{userId}`
- `POST /api/gym-management/user-roles`

### Gyms
- `GET /api/gym-management/gyms`
- `GET /api/gym-management/gyms/{gymId}`
- `POST /api/gym-management/gyms`
- `PUT /api/gym-management/gyms/{gymId}`
- `DELETE /api/gym-management/gyms/{gymId}`

### Gym plans
- `GET /api/gym-management/gyms/{gymId}/plans`
- `POST /api/gym-management/gyms/{gymId}/plans`
- `PUT /api/gym-management/gyms/{gymId}/plans/{planId}`
- `DELETE /api/gym-management/gyms/{gymId}/plans/{planId}`

### Gym staff
- `GET /api/gym-management/gyms/{gymId}/staff`
- `POST /api/gym-management/gyms/{gymId}/staff`
- `DELETE /api/gym-management/gyms/{gymId}/staff/{staffId}`

### Gym clients
- `GET /api/gym-management/gyms/{gymId}/clients`
- `POST /api/gym-management/gyms/{gymId}/clients`
- `PUT /api/gym-management/gyms/{gymId}/clients/{clientId}/trainer`
- Gym-client flow is direct CRUD by owner/receptionist/staff (no invite email step)

### Trainer plans
- `GET /api/gym-management/trainers/{trainerId}/plans`
- `POST /api/gym-management/trainers/{trainerId}/plans`
- `PUT /api/gym-management/trainers/{trainerId}/plans/{planId}`
- `DELETE /api/gym-management/trainers/{trainerId}/plans/{planId}`

### Trainer clients
- `GET /api/gym-management/trainers/{trainerId}/clients`
- `POST /api/gym-management/trainers/{trainerId}/clients`
- `POST /api/gym-management/trainers/{trainerId}/clients/invites/{clientEmail}`
- `POST /api/gym-management/trainer-client-invites/accept`
- `PUT /api/gym-management/trainers/{trainerId}/clients/{clientId}/transfer`

## End-to-end flow

1. Auth middleware provisions current user (`Authorization` domain) and assigns default `IndependentClient` role
2. User can receive one or many platform roles in `GymManagement`
3. A gym owner creates gym and gym plans
4. Owner/receptionist manages gym staff and enrolls clients to gym plans
5. Gym trainers can be assigned/reassigned to gym clients
6. Independent trainers can invite clients by email token and/or manage already-linked clients
7. Invite generation sends template email with `trainer_name` and encoded `register_url` variables
8. Client opens register URL, signs up/logs in, and submits the encoded invite payload to `POST /api/gym-management/trainer-client-invites/accept`
9. Accept handler decodes payload (`trainerId` + token), validates invite ownership/expiration, and creates the `TrainerClients` row
10. Trainer plan assignment for independent clients is optional at invite/creation time and can happen later
11. Client ownership can be transferred between trainers ("steal" flow)
12. When linked to an independent trainer, user receives internal `Client` role; when linked to a gym, user receives internal `GymClient` role
13. Internal roles are exclusive across trainer/gym relationship flows

## ASCII diagram

```
+--------------------------+             +-----------------------+
| Authorization Middleware |             | GymManagement Domain  |
| (current user context)   |             | (CQRS + Result)       |
+------------+-------------+             +-----------+-----------+
             |                                       |
             v                                       v
      +------+----------------------+      +---------+-------------------------+
      | UserPlatformRoles           |      | PlatformTiers                    |
      | (Trainer/Client/GymOwner)*  |----->| ShapeUp subscription tiers       |
      +------+----------------------+      +----------------+------------------+
             |                                            |
             |                                            |
             v                                            v
      +------+---------------------+            +---------+--------------------+
      | Gyms (OwnerId)             |----------->| GymPlans                     |
      +------+---------------------+            +-----------------------------+
             |
             | has staff
             v
      +------+---------------------+            +------------------------------+
      | GymStaff                   |<-----------| GymClients                    |
      | Trainer / Receptionist     | assign     | Plan + Optional Trainer       |
      +----------------------------+            +------------------------------+

      +----------------------------+            +------------------------------+
      | TrainerPlans (independent) |<-----------| TrainerClients                |
      | owned by TrainerId         | transfer   | one client -> one trainer      |
      +----------------------------+            +------------------------------+

* assignable: Trainer/IndependentClient/GymOwner
* internal: Client (trainer link), GymClient (gym link)
* Client and GymClient are mutually exclusive
```

