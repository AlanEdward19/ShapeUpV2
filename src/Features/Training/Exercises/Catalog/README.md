# Catálogo de exercícios

18/10/2026 — expansão aditiva via20261018190000_ExpandExerciseCatalog.

-28 opções adicionais: braços, peito, costas, ombros, pernas, panturrilhas e abdômen.
- Cada opção nova tem nome português, descrição, duas instruções, equipamento e perfis.
- Não recria nomes ingleses ou portugueses existentes, nem substitui cadastros editados.
- Perfis são pesos editoriais de destaque; não representam medição fisiológica individual.
- Os nomes portugueses são resolvidos em CreateExerciseHandler.MapResponse, incluindo grupos.
- Nenhum contrato/enum/rota foi alterado; GETs mantêm a paginação existente.
- O app resolve aliases antigos, acentos e flags numéricos em uma taxonomia compartilhada.
- Rollback preserva registros para manter referências de treinos e sessões.
- Migração aplicada em18/10/2026 ao Development local(ShapeUpDb):64→92;28 novos completos e64 anteriores preservados. Outros ambientes não alterados.

Provas: ExpandedExerciseCatalogTests em UnitTests e IntegrationTests.
A integração usa SQL Server descartável: execução repetida e preservação de registro prévio.

Referência de movimentos: https://www.acefitness.org/resources/everyone/exercise-library/
Descrições escritas para o projeto; não copia fotos, vídeos ou textos licenciados.
