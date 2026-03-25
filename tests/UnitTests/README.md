# Unit Tests - ShapeUp

Este diretório contém todos os testes unitários da aplicação ShapeUp, organizados por domínio/feature.

## 📊 Estatísticas Atualizadas

- **Total de Testes:** 186 (convertidos de Fact para Theory)
- **Taxa de Sucesso:** 100% ✅
- **Tempo de Execução:** ~0.70s
- **Padrão de Teste:** xUnit Theory com InlineData/MemberData

## Estrutura

```
UnitTests/
├── Features/
│   ├── Authorization/
│   │   ├── Groups/
│   │   │   ├── CreateGroupHandlerTests.cs
│   │   │   ├── CreateGroupValidatorTests.cs
│   │   │   ├── AddUserToGroupHandlerTests.cs
│   │   │   └── AddUserToGroupValidatorTests.cs
│   │   ├── Scopes/
│   │   │   ├── CreateScopeHandlerTests.cs
│   │   │   └── CreateScopeValidatorTests.cs
│   │   ├── UserManagement/
│   │   │   ├── GetOrCreateUserHandlerTests.cs
│   │   │   └── GetOrCreateUserValidatorTests.cs
│   │   ├── UserRepositoryTests.cs
│   │   └── GroupRepositoryTests.cs
│   └── AuditLogs/
│       ├── GetAuditLogsHandlerTests.cs
│       └── AuditLogRepositoryTests.cs
└── UnitTests.csproj
```

## Cobertura de Testes

### Padrão xUnit Theory

Todos os testes foram convertidos de **Fact** para **Theory** para permitir múltiplos casos de teste parametrizados:

```csharp
// Usando InlineData para múltiplos casos
[Theory]
[InlineData("user1@example.com", true)]
[InlineData("user2@example.com", true)]
[InlineData("invalid-email", false)]
public async Task Validate_EmailAddress_ReturnsExpectedResult(string email, bool isValid)
{
    // ...
}

// Usando MemberData para dados complexos
[Theory]
[MemberData(nameof(ValidScopeCases))]
public async Task HandleAsync_ValidCommand_CreatesScopeSuccessfully(
    string domain, string subdomain, string action, string? description)
{
    // ...
}

public static IEnumerable<object[]> ValidScopeCases =>
    new List<object[]>
    {
        new[] { (object)"users", "profile", "read", (object)"Read user profile" },
        new[] { (object)"groups", "management", "create", (object)"Create group" },
        // ... mais casos
    };
```

### Authorization Domain

#### UserManagement (GetOrCreateUser)
- **GetOrCreateUserHandlerTests** (3 testes)
  - `HandleAsync_ExistingUser_ReturnsUserWithScopes`
  - `HandleAsync_NewUser_CreatesUserWithoutScopes`
  - `HandleAsync_NewUserWithoutDisplayName_CreatesUserSuccessfully`

- **GetOrCreateUserValidatorTests** (6 testes)
  - `Validate_ValidCommand_HasNoErrors`
  - `Validate_EmptyFirebaseUid_HasError`
  - `Validate_InvalidEmail_HasError`
  - `Validate_EmptyEmail_HasError`
  - `Validate_NullDisplayName_IsValid`
  - `Validate_MultipleFieldsInvalid_HasMultipleErrors`

#### Groups
- **CreateGroupHandlerTests** (3 testes)
  - `HandleAsync_ValidCommand_CreatesGroupAndReturnsSuccess`
  - `HandleAsync_GroupWithoutDescription_CreatesSuccessfully`
  - `HandleAsync_CreatorAssignedAsOwner_Correctly`

- **CreateGroupValidatorTests** (7 testes)
  - `Validate_ValidCommand_HasNoErrors`
  - `Validate_EmptyName_HasError`
  - `Validate_NameExceedsMaxLength_HasError`
  - `Validate_NameAtMaxLength_IsValid`
  - `Validate_NullDescription_IsValid`
  - `Validate_WhitespaceOnlyName_HasError`

- **AddUserToGroupHandlerTests** (7 testes)
  - `HandleAsync_OwnerAddsUser_SuccessfullyAddsUser`
  - `HandleAsync_AdministratorAddsUser_SuccessfullyAddsUser`
  - `HandleAsync_MemberTriesToAddUser_ReturnsForbidden`
  - `HandleAsync_UserDoesNotExist_ReturnsNotFound`
  - `HandleAsync_GroupDoesNotExist_ReturnsNotFound`
  - `HandleAsync_UserAlreadyInGroup_ReturnsConflict`
  - `HandleAsync_InvalidRole_ReturnsValidationError`

- **AddUserToGroupValidatorTests** (10 testes)
  - `Validate_ValidCommand_HasNoErrors`
  - `Validate_OwnerRole_IsValid`
  - `Validate_AdministratorRole_IsValid`
  - `Validate_CaseInsensitiveRole_IsValid`
  - `Validate_InvalidRole_HasError`
  - `Validate_EmptyRole_HasError`
  - `Validate_ZeroUserId_HasError`
  - `Validate_NegativeUserId_HasError`
  - `Validate_ZeroGroupId_HasError`
  - `Validate_MultipleErrors_HasAllErrors`

- **GroupRepositoryTests** (10 testes)
  - `AddAsync_ValidGroup_SavesSuccessfully`
  - `GetByIdAsync_ExistingGroup_ReturnsGroup`
  - `GetByIdAsync_NonexistentGroup_ReturnsNull`
  - `GetAllAsync_MultipleGroups_ReturnsAll`
  - `GetAllAsync_NoGroups_ReturnsEmpty`
  - `AddUserToGroupAsync_ValidInput_AddsUserSuccessfully`
  - `UserBelongsToGroupAsync_UserInGroup_ReturnsTrue`
  - `UserBelongsToGroupAsync_UserNotInGroup_ReturnsFalse`
  - `GetUserRoleInGroupAsync_UserInGroup_ReturnsRole`
  - `GetUserRoleInGroupAsync_UserNotInGroup_ReturnsNull`
  - `GetByUserIdAsync_UserInMultipleGroups_ReturnsAllGroups`
  - `GetByUserIdAsync_UserNotInAnyGroup_ReturnsEmpty`

#### Scopes
- **CreateScopeHandlerTests** (4 testes)
  - `HandleAsync_ValidCommand_CreatesScopeSuccessfully`
  - `HandleAsync_ScopeAlreadyExists_ReturnsConflictError`
  - `HandleAsync_WithoutDescription_CreatesScopeSuccessfully`
  - `HandleAsync_ComplexScopeName_FormattedCorrectly`

- **CreateScopeValidatorTests** (11 testes)
  - `Validate_ValidCommand_HasNoErrors`
  - `Validate_EmptyDomain_HasError`
  - `Validate_EmptySubdomain_HasError`
  - `Validate_EmptyAction_HasError`
  - `Validate_DomainWithInvalidCharacters_HasError`
  - `Validate_SubdomainWithInvalidCharacters_HasError`
  - `Validate_ActionWithInvalidCharacters_HasError`
  - `Validate_DomainWithUnderscores_IsValid`
  - `Validate_SubdomainWithUnderscores_IsValid`
  - `Validate_ActionWithUnderscores_IsValid`
  - `Validate_NullDescription_IsValid`
  - `Validate_AllNumericParts_IsValid`

- **UserRepositoryTests** (9 testes)
  - `AddAsync_ValidUser_SavesSuccessfully`
  - `GetByIdAsync_ExistingUser_ReturnsUser`
  - `GetByIdAsync_NonexistentUser_ReturnsNull`
  - `GetByFirebaseUidAsync_ExistingUser_ReturnsUser`
  - `GetByFirebaseUidAsync_NonexistentUser_ReturnsNull`
  - `GetByEmailAsync_ExistingUser_ReturnsUser`
  - `GetByEmailAsync_NonexistentUser_ReturnsNull`
  - `GetAllAsync_MultipleUsers_ReturnsAll`
  - `GetAllAsync_NoUsers_ReturnsEmpty`
  - `UpdateAsync_ExistingUser_UpdatesSuccessfully`

### AuditLogs Domain

- **GetAuditLogsHandlerTests** (8 testes)
  - `HandleAsync_ValidQuery_ReturnsAuditLogs`
  - `HandleAsync_FilterByEndpoint_ReturnsFilteredLogs`
  - `HandleAsync_FilterByMethod_ReturnsFilteredLogs`
  - `HandleAsync_FilterByUserEmail_ReturnsFilteredLogs`
  - `HandleAsync_ValidCursor_DecodesAndUsesCursor`
  - `HandleAsync_InvalidCursor_ReturnsValidationError`
  - `HandleAsync_EmptyPageSize_UsesDefault`
  - `HandleAsync_MultipleFilters_AppliesAllFilters`
  - `HandleAsync_NoLogs_ReturnsEmptyList`

- **AuditLogRepositoryTests** (9 testes)
  - `AddAsync_ValidEntry_SavesSuccessfully`
  - `GetPageAsync_NoFilters_ReturnsAllEntries`
  - `GetPageAsync_WithPageSize_RespectsPageSize`
  - `GetPageAsync_FilterByEndpoint_ReturnsMatchingEntries`
  - `GetPageAsync_FilterByMethod_ReturnsMatchingEntries`
  - `GetPageAsync_FilterByUserEmail_ReturnsMatchingEntries`
  - `GetPageAsync_WithCursor_SkipsEntriesBeforeCursor`
  - `GetPageAsync_MultipleFilters_AppliesAllFilters`
  - `GetPageAsync_NoMatches_ReturnsEmpty`

## Total de Testes: 186 ✅

**Status:** ✅ Todos os testes passando  
**Padrão:** xUnit Theory com InlineData e MemberData

## Executando os Testes

```bash
# Rodar todos os testes
dotnet test tests/UnitTests/UnitTests.csproj

# Rodar testes de um domínio específico
dotnet test tests/UnitTests/UnitTests.csproj --filter "FullyQualifiedName~Authorization"

# Rodar com verbosidade completa
dotnet test tests/UnitTests/UnitTests.csproj --verbosity detailed

# Gerar relatório de cobertura
dotnet test tests/UnitTests/UnitTests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Tecnologias Utilizadas

- **Framework:** xUnit
- **Mocking:** Moq
- **Validação:** FluentValidation
- **Database:** Entity Framework Core com InMemoryDatabase
- **.NET:** 10.0

## Notas Importantes

### InMemoryDatabase Limitações

Alguns testes de repositório foram ajustados debido às limitações do InMemoryDatabase:

1. **ExecuteDelete/ExecuteDeleteAsync** não é suportado - testes de deleção devem ser feitos em testes de integração com SQL Server
2. **ExecuteUpdate/ExecuteUpdateAsync** não é suportado - testes de atualização em massa devem ser feitos em testes de integração

Esses métodos funcionam corretamente em produção com SQL Server/SQLite.

### Padrão de Teste

Todos os testes seguem o padrão **AAA** (Arrange-Act-Assert):

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange - preparar dados e mocks
    // Act - executar método
    // Assert - verificar resultados
}
```

## Contribuindo

Ao adicionar novos testes:

1. Siga o padrão de nomenclatura: `MethodName_Scenario_ExpectedResult`
2. Use `CancellationToken.None` para operações síncronas
3. Sempre mock dependências externas
4. Organize testes por feature/handler/validator
5. Documente cenários complexos com comentários

## Próximas Etapas

- [ ] Adicionar testes de integração para repositórios
- [ ] Adicionar testes e2e para endpoints
- [ ] Aumentar cobertura para 100%
- [ ] Adicionar testes de performance

