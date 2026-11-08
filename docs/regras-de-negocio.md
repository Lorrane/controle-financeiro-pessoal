# Regras de negócio

## Categorias

- O nome é obrigatório e limitado a 80 caracteres.
- A cor é opcional e, quando informada, usa o formato hexadecimal `#RRGGBB`.
- A interface deriva tons do valor cadastrado; categoria sem cor usa paleta
  neutra.
- Categorias são inativadas, não apagadas fisicamente.

## Contas

- Nome obrigatório, até 120 caracteres; valor previsto estritamente positivo;
  descrição opcional, até 500 caracteres.
- Cada conta pertence a uma categoria ativa no cadastro ou edição.
- O tipo e a numeração inicial do parcelamento não podem ser alterados após a
  criação.
- Contas são inativadas, não apagadas fisicamente.

### Tipos e compromissos

- **Única:** gera exatamente um compromisso. O vencimento é opcional; quando
  ausente, o compromisso permanece sem vencimento.
- **Parcelada:** exige vencimento, total de parcelas positivo e parcela atual
  entre 1 e o total. Gera somente da parcela atual até a última; parcelas
  anteriores não são criadas automaticamente.
- **Fixa mensal:** exige vencimento e não possui limite final. Gera ocorrências
  dentro dos próximos 12 meses, renovadas quando a conta ativa é consultada.
- Cada compromisso é associado à conta original e tem sequência única dentro
  dela. Consultas repetidas não duplicam ocorrências.
- Vencimentos nos dias 29, 30 e 31 preservam o dia de referência; meses menores
  usam seu último dia sem alterar a referência dos meses seguintes.

### Edição e histórico

- Alterações de valor, categoria e vencimento atingem apenas compromissos
  futuros ou sem vencimento, pendentes, não cancelados e sem histórico
  financeiro.
- Valores e vencimentos anteriores são registrados para rastreabilidade.
- Compromissos vencidos ou com histórico não são reescritos.
- Ao ajustar uma conta mensal, a sequência recorrente permanece estável.
  Reancoragem antes da primeira sequência é rejeitada.
- Compromissos fora da programação podem ser cancelados, mas permanecem
  persistidos.

### Inativação

- Inativar a conta impede novas ocorrências automáticas e mantém todos os
  compromissos registrados.
- Compromissos futuros sem histórico podem ser cancelados com motivo e data.
- `Compromisso.Cancelar` não altera compromissos pagos, cancelados ou que
  possuam histórico financeiro.
- Compromissos vencidos não são alterados retroativamente pela inativação.

### Estados independentes

O status de pagamento (`pendente` ou `pago`) não substitui a classificação de
vencimento (`semVencimento`, `emDia` ou `atrasado`). Nesta etapa, não existe
ação de pagamento nem marcação automática como pago.

## Apresentação da lista

A lista carrega contas ativas e inativas, e aplica filtros localmente. Por
padrão, exibe somente contas ativas. Cada cartão mostra até dois compromissos,
priorizando pendentes não cancelados, com atrasados antes de futuros e
vencimentos mais antigos primeiro. A expansão é apenas visual e permite
consultar todos os registros sem modificar seus estados.

Pagamentos, movimentações de caixa, metas, previsões e relatórios não fazem
parte desta etapa.
