# GymManagement Architecture

## Domain scope

`GymManagement` handles business rules for:
- Platform subscription tiers (ShapeUp plans)
- User role composition (same user can be `Trainer`, `Client`, `GymOwner` simultaneously)
- Gym operation model (owner, receptionist, trainer, client)
- Gym-owned plans and trainer-owned plans
- Client assignment rules inside gyms and for independent trainers

## Database structure

Context: `Features/GymManagement/Infrastructure/Data/GymManagementDbContext.cs`

Main tables:
- `PlatformTiers`
  - Platform plans, dynamic CRUD
- `UserPlatformRoles`
  - N-role model per user (`UserId + Role` unique)
  - Optional `PlatformTierId`
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

### Trainer plans
- `GET /api/gym-management/trainers/{trainerId}/plans`
- `POST /api/gym-management/trainers/{trainerId}/plans`
- `PUT /api/gym-management/trainers/{trainerId}/plans/{planId}`
- `DELETE /api/gym-management/trainers/{trainerId}/plans/{planId}`

### Trainer clients
- `GET /api/gym-management/trainers/{trainerId}/clients`
- `POST /api/gym-management/trainers/{trainerId}/clients`
- `PUT /api/gym-management/trainers/{trainerId}/clients/{clientId}/transfer`

## End-to-end flow

1. Auth middleware provisions current user (`Authorization` domain)
2. User can receive one or many platform roles in `GymManagement`
3. A gym owner creates gym and gym plans
4. Owner/receptionist manages gym staff and enrolls clients to gym plans
5. Gym trainers can be assigned/reassigned to gym clients
6. Independent trainers create trainer plans and manage their own clients
7. Client ownership can be transferred between trainers ("steal" flow)

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
      | owned by TrainerId         | transfer   | one trainer -> many clients   |
      +----------------------------+            +------------------------------+

* same user can have multiple roles
```

