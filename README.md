# PARTE 01
# Questão A
**Pergunta:** Qual/Quais API(s) você criaria para que receber esses dados? Justifique sua resposta.
**Resposta:**
Eu criaria uma única API (Endpoint) do tipo **POST** que aceita como corpo da requisição uma **lista (array JSON)** de objetos de medição. Algo assim:
`POST /api/measurements`
Body: `[ { ... }, { ... } ]`
Primeiro por eficiência de rede, já que o sensor acumula dados quando a rede está intermitente. Enviar esses dados acumulados em um único pacote (lote) é muito mais eficiente do que fazer múltiplas requisições pequenas(reduz o overhead de handshake HTTP/TCP e economiza bateria/processamento do sensor). Em segundo lugar, pela robustez, o que permite que o servidor processe o lote em uma única transação de banco de dados, garantindo que todos os dados do período sejam salvos corretamente ou rejeitados em conjunto caso haja erro de validação grave. Por último , pela simplicidade. Um único endpoint pode tratar tanto o caso "normal" (lista com 1 item) quanto o caso "recuperação de falha" (lista com N itens), simplificando a lógica do cliente (sensor) e do servidor.

# Questão B
**Pergunta:** Como você definiria o objeto de request dessa API no Asp.Net Core? (Exemplifique com código)
**Resposta:**
Eu definiria uma classe (DTO ou Model) representando a estrutura de um único registro de medição, utilizando os tipos de dados apropriados do C# para mapear os requisitos (Int32, String, DateTimeOffset, Decimal).

```csharp
public class MeasurementRequest
{
    public int Id { get; set; }
    public string Codigo { get; set; }
    public DateTimeOffset DataHoraMedicao { get; set; }
    public decimal Medicao { get; set; }
}
```
No Controller, o parâmetro seria uma lista (ou `IEnumerable`) dessa classe para suportar o envio em lote:

```csharp
[HttpPost]
public async Task<IActionResult> Post([FromBody] List<MeasurementRequest> measurements)
{
    // ... processamento
}
```

# Questão C
**Pergunta:** Qual a melhor alternativa de banco de dados para esse cenário na sua opinião? Justifique sua resposta.
**Resposta:**
A melhor alternativa seria um Banco de Dados de Séries Temporais (TSDB), como o TimescaleDB (baseado no PostgreSQL) ou InfluxDB. Com base na natureza dos dados, o cenário trata de monitoramento contínuo, onde o dado mais importante é o valor medido associado a um carimbo de tempo (*timestamp*). TSDBs são otimizados exatamente para esse padrão de escrita intensa (append-only). Além disso, os sensores podem gerar grandes volumes de dados, e os TSDBs mantêm a performance de ingestão estável mesmo com bilhões de registros, enquanto bancos tradicionais podem sofrer com a indexação em tabelas grandes, o que garante a performance. Em termos gerais, para consultas analíticas de monitoramento, geralmente queremos saber médias, máximos, mínimos em janelas de tempo (ex: "média da temperatura na última hora"). TSDBs possuem funções nativas e otimizadas para essas agregações, sendo muito mais rápidos que SQL tradicional. Por fim, od TSDBs facilitam o descarte de dados antigos (retention policies) ou a compactação de dados históricos, o que é essencial para manter o custo de armazenamento controlado em sistemas de IoT, favorecendo o ciclo de vida dos dados.

# PARTE 02
# Questão A
**Pergunta:** Uma API onde se possa vincular um Setor/Equipamento à um sensor; (Exemplifique com código)
**Resposta:**
Para vincular um Sensor a um Equipamento, criei um relacionamento no banco de dados (chave estrangeira) e um endpoint específico `POST /api/sensors/link` que recebe o código do sensor e o ID do equipamento.

Modelo (Sensor.cs):
```csharp
public class Sensor
{
    public int Id { get; set; }
    public string Code { get; set; }
    
    // Chave estrangeira para Equipamento
    public int? EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }
}
```
Controller (SensorsController.cs):
```csharp
[HttpPost("link")]
public async Task<IActionResult> LinkSensorToEquipment([FromBody] LinkSensorRequest request)
{
    // Busca o sensor pelo código
    var sensor = await _context.Sensors.FirstOrDefaultAsync(s => s.Code == request.SensorCode);
    if (sensor == null) return NotFound("Sensor not found");

    // Busca o equipamento pelo ID
    var equipment = await _context.Equipments.FindAsync(request.EquipmentId);
    if (equipment == null) return NotFound("Equipment not found");

    // Realiza o vínculo
    sensor.Equipment = equipment;
    await _context.SaveChangesAsync();

    return Ok(new { message = "Sensor linked successfully" });
}

public class LinkSensorRequest
{
    public string SensorCode { get; set; }
    public int EquipmentId { get; set; }
}
```

# Questão B
**Pergunta:** Uma API onde ao informar o Id de um Setor/Equipamento traga as últimas 10 medições de cada sensor vinculado a ele; (Exemplifique com código)
**Resposta:**
Implementei um endpoint `GET /api/equipments/{id}/measurements` que busca o equipamento, lista seus sensores e, para cada um, recupera as 10 medições mais recentes.

DTOs de Resposta:
```csharp
public class EquipmentMeasurementsDto
{
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; }
    public List<SensorMeasurementsDto> Sensors { get; set; } = new();
}

public class SensorMeasurementsDto
{
    public int SensorId { get; set; }
    public string SensorCode { get; set; }
    public List<Measurement> Measurements { get; set; } = new();
}
```

Controller (EquipmentsController.cs):
```csharp
[HttpGet("{id}/measurements")]
public async Task<ActionResult<EquipmentMeasurementsDto>> GetEquipmentMeasurements(int id)
{
    var equipment = await _context.Equipments.FindAsync(id);
    if (equipment == null) return NotFound();

    var sensors = await _context.Sensors
        .Where(s => s.EquipmentId == id)
        .ToListAsync();

    var result = new EquipmentMeasurementsDto
    {
        EquipmentId = equipment.Id,
        EquipmentName = equipment.Name,
        Sensors = new List<SensorMeasurementsDto>()
    };

    foreach (var sensor in sensors)
    {
        var measurements = await _context.Measurements
            .Where(m => m.Codigo == sensor.Code)
            .OrderByDescending(m => m.DataHoraMedicao)
            .Take(10)
            .ToListAsync();

        result.Sensors.Add(new SensorMeasurementsDto
        {
            SensorId = sensor.Id,
            SensorCode = sensor.Code,
            Measurements = measurements
        });
    }

    return result;
}
```

# PARTE 03
# Questão A
**Pergunta:** Qual seria a sua proposta para resolver esse problema? Quais tecnologias você optaria por usar para resolver esse problema? Justifique sua resposta.
**Resposta:**
Minha proposta é utilizar uma Arquitetura Orientada a Eventos (Event-Driven Architecture) com processamento assíncrono em segundo plano. O fluxo de dados seria:
1.  Ingestão: A API recebe as medições e as persiste no banco de dados (como já implementado).
2.  Publicação: Após salvar, a API publica um evento (ex: `MeasurementsReceived`) em uma fila de mensagens (Message Queue).
3.  Processamento: Um serviço dedicado (Worker) consome essas mensagens.
4.  Verificação de Regras:
    *   Regra Crítica: O Worker consulta as últimas 5 medições. Se todas forem < 1 ou > 50, enfileira um envio de e-mail de alerta crítico.
    *   Regra de Atenção: O Worker consulta as últimas 50 medições e calcula a média. Se a média estiver na margem de erro (ex: entre -1 e 3, ou entre 48 e 52), enfileira um e-mail de atenção.
5.  Notificação: Um serviço de e-mail processa os envios.

 As tecnologias escolhidas seriam:
*   RabbitMQ ou Azure Service Bus: Para o sistema de mensageria porque garante que a API de ingestão não seja bloqueada pelo processamento das regras e que nenhuma mensagem seja perdida em caso de pico de tráfego.
*   .NET BackgroundService (Worker): Para criar o consumidor que processa as regras de negócio de forma assíncrona e contínua.
*   Redis (Cache): Para armazenar janelas de dados recentes (ex: as últimas 50 medições de cada sensor) para leitura rápida, evitando sobrecarregar o banco de dados principal com consultas repetitivas de agregação.
A principal razão das minhas escolhas é a Performance e Escalabilidade. Calcular médias móveis e verificar sequências a cada requisição HTTP pode aumentar muito a latência da API, especialmente com milhares de sensores. O processamento assíncrono isola a lógica pesada da ingestão de dados. O uso de filas garante resiliência (se o serviço de e-mail cair, o processamento tenta novamente depois).

# Questão B
**Pergunta:** Implemente o algoritmo necessário para resolver esse problema. (Exemplifique com código)
**Resposta:**
Implementei um serviço `AlertService` que encapsula a lógica de verificação das regras.
Serviço (AlertService.cs):
```csharp
public enum AlertType { None, Critical, Attention }

public class AlertService
{
    private const int CriticalConsecutiveCount = 6; // Mais de 5
    private const int AttentionWindowSize = 50;

    public AlertType CheckAlerts(List<Measurement> history)
    {
        if (history == null || !history.Any()) return AlertType.None;

        var sortedHistory = history.OrderByDescending(m => m.DataHoraMedicao).ToList();

        // 1. Regra Crítica: > 5 medições consecutivas < 1 ou > 50
        if (sortedHistory.Count >= CriticalConsecutiveCount)
        {
            var lastN = sortedHistory.Take(CriticalConsecutiveCount);
            if (lastN.All(m => m.Medicao < 1 || m.Medicao > 50))
                return AlertType.Critical;
        }

        // 2. Regra de Atenção: Média das últimas 50 na margem de erro (+/- 2)
        // Margem Inferior: 1 +/- 2 => [-1, 3]
        // Margem Superior: 50 +/- 2 => [48, 52]
        if (sortedHistory.Count >= AttentionWindowSize)
        {
            var window = sortedHistory.Take(AttentionWindowSize);
            decimal average = window.Average(m => m.Medicao);

            bool inRange1 = average >= -1 && average <= 3;
            bool inRange2 = average >= 48 && average <= 52;

            if (inRange1 || inRange2)
                return AlertType.Attention;
        }

        return AlertType.None;
    }
}
```

# Questão C
**Pergunta:** Crie testes unitários com os cenários de testes que você acredita serem necessários para validar sua solução.
**Resposta:**
Criei um projeto de testes `IotMonitoringApi.Tests` utilizando xUnit para validar os cenários.
Cenários Testados:
1.  Critical: Últimas 6 medições abaixo de 1.
2.  Critical: Últimas 6 medições acima de 50.
3.  None: Apenas 5 medições críticas (não deve alertar).
4.  Attention: Média das últimas 50 próxima de 0 (margem inferior).
5.  Attention: Média das últimas 50 próxima de 50 (margem superior).
6.  None: Média normal (ex: 25).

Exemplo de Teste (AlertServiceTests.cs):
```csharp
[Fact]
public void CheckAlerts_ShouldReturnCritical_WhenLast6MeasurementsAreBelow1()
{
    var history = new List<Measurement>();
    for (int i = 0; i < 6; i++)
        history.Add(new Measurement { Medicao = 0.5m, DataHoraMedicao = DateTimeOffset.Now });

    var result = _service.CheckAlerts(history);
    Assert.Equal(AlertType.Critical, result);
}

[Fact]
public void CheckAlerts_ShouldReturnAttention_WhenAverageIsWithinLowMargin()
{
    var history = new List<Measurement>();
    for (int i = 0; i < 50; i++)
        history.Add(new Measurement { Medicao = 0m, DataHoraMedicao = DateTimeOffset.Now });

    var result = _service.CheckAlerts(history);
    Assert.Equal(AlertType.Attention, result);
}
```

# PARTE 04
**Pergunta:** Após implementar o sistema em um cliente de grande porte... o sistema estava gerando um atraso na leitura e no processamento dos dados recebidos pelos sensores. Qual(ais) solução(ões) você indicaria para resolver esse problema?
**Resposta:**
Para resolver o problema de latência e gargalo no processamento devido ao alto volume de dados, indicaria as seguintes soluções escaláveis:
1.  Uso de Mensageria (Message Broker) de Alta Performance:
    *   Introduzir um Kafka ou RabbitMQ logo na entrada. A API apenas recebe o dado e joga na fila (operação extremamente rápida). O processamento pesado (regras de negócio, validações, escrita no banco) é feito por consumidores (Workers) em segundo plano, desacoplando a ingestão do processamento.
2.  Escalabilidade Horizontal (Scale Out):
    *   Aumentar o número de instâncias da API e dos Workers. Com o uso de Kubernetes (K8s) ou Azure App Service, podemos ter múltiplas réplicas da aplicação rodando em paralelo, distribuindo a carga.
3.  Sharding de Banco de Dados ou TSDB:
    *   Se o gargalo for o banco de dados, utilizar Sharding (particionamento horizontal dos dados) ou migrar definitivamente para um Time Series Database (TSDB), que são projetados para ingerir milhões de pontos de dados por segundo sem travar.
4.  Processamento em Lote (Batch Processing):
    *   Em vez de processar cada medição individualmente, os Workers podem processar micro-lotes (ex: agrupar 100 medições antes de salvar no banco), reduzindo drasticamente o I/O de disco e rede.
5.  Cache Distribuído (Redis):
    *   Utilizar Redis para armazenar dados quentes (ex: últimas medições para verificação de alertas), evitando consultas repetitivas ao banco de dados principal.
