# Roteiro da apresentação (3 a 5 minutos)

**Rodrigo Nakata — RM 556417**

> Dica: antes de gravar, apague `src/CadastroProdutos/bin/Debug/net8.0/produtos.db` e a pasta `logs`
> para começar com o banco vazio. Deixe o VS Code (ou o Visual Studio) e um terminal abertos lado a lado.

---

### 0:00 – 0:30 | Introdução
- "Olá, sou o Rodrigo Nakata, RM 556417. Este é o Cadastro de Produtos em C# com ADO.NET e SQLite."
- Mostre rapidamente a estrutura de pastas: `Models`, `Data`, `UI`, `Logging`, `database/script.sql` e `tests`.

### 0:30 – 1:15 | Banco e configuração
- Abra o `database/script.sql`: tabela `Produtos` com Id, Nome, Preco, Estoque e Categoria, mais as colunas
  `Ativo` e `DataExclusao` para o soft delete e os `CHECK` constraints.
- Abra o `appsettings.json` e mostre a connection string.
- No `Program.cs`: lê a configuração → `DatabaseInitializer` executa o script → cria o `ProdutoRepository` → abre o menu.

### 1:15 – 2:15 | Código do Repository (o ponto principal)
- Abra o `ProdutoRepository.cs`:
  - **Inserir**: SQL com `@Nome, @Preco...` → `Parameters.Add(...)` → `ExecuteNonQuery()` dentro de **transação**.
  - **Listar / BuscarPorId**: `ExecuteReader()` + `MapearProduto(leitor)` (mapeamento manual com `GetOrdinal`).
  - **Atualizar / Excluir**: `ExecuteNonQuery()` e verificação das linhas afetadas.
  - **TratarErro**: captura `SqliteException`, grava no log e devolve uma mensagem amigável.
- Destaque: o `MenuProdutos` não tem nenhum SQL, e o Repository não usa o console. Essa é a separação das camadas.

### 2:15 – 4:00 | Demonstração (`dotnet run --project src/CadastroProdutos`)
1. **Inserir** 2 ou 3 produtos. Mostre a validação digitando um preço negativo e um nome vazio.
2. **Inserir** um produto com o nome `x'); DROP TABLE Produtos; --` → mostre que ele foi salvo como texto (SQL Injection evitado).
3. **Listar** os produtos.
4. **Buscar por ID**, primeiro um que existe e depois o ID 99, que não existe.
5. **Atualizar** um produto, pressionando ENTER para manter alguns campos.
6. **Excluir** com exclusão lógica → **Listar** com "S" para mostrar o produto marcado como "Excluído" → **Restaurar** (opção 5 → 3).
7. **Sair**.

### 4:00 – 4:45 | Log e testes
- Abra o `logs/operacoes.log` e mostre cada operação registrada com data e hora.
- Rode `dotnet test` e mostre os 26 testes passando: CRUD, SQL Injection, soft delete, rollback e erros.

### 4:45 – 5:00 | Encerramento
- "Foram atendidos o CRUD completo com SQL parametrizado, a separação entre interface e Repository, o tratamento de erros e o log em arquivo,
  além dos bônus de soft delete, transações e testes unitários. Obrigado!"
