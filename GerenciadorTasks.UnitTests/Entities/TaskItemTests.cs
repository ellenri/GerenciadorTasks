using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Enums;
using GerenciadorTasks.Core.Exceptions;
using TaskStatus = GerenciadorTasks.Core.Enums.TaskStatus;

namespace GerenciadorTasks.UnitTests.Entities;

/// <summary>
/// Testes da entidade TaskItem (a "missão").
/// </summary>
public class TaskItemTests
{
    // Valores reutilizáveis para montar uma missão válida.
    // static readonly: inicializados uma vez (eficiência) e imutáveis.
    private static readonly Guid ValidChildId = Guid.NewGuid();
    private static readonly Guid ValidCreatorId = Guid.NewGuid();
    private static readonly DateOnly FutureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

    /// Helper de fábrica: cria uma missão válida com parâmetros opcionais.
    /// Centralizar a criação reduz duplicação (DRY) e deixa cada teste focado no que ele valida.
    private static TaskItem CreateValidTask(
        string title = "Derrotar o Dragão da Matemática",
        TaskPriority priority = TaskPriority.Medium)
        => new(title, TaskCategory.School, priority, FutureDate, new TimeOnly(14, 0),
               ValidChildId, ValidCreatorId);

    [Fact]
    public void Constructor_WithValidData_ShouldCreatePendingTask()
    {
        // Act
        var task = CreateValidTask();

        // Assert
        Assert.Equal(TaskStatus.Pending, task.Status);  // toda missão nova nasce pendente
        Assert.Null(task.CompletedAt);
        Assert.NotEqual(Guid.Empty, task.Id);
        Assert.Equal(ValidChildId, task.AssignedToId);
    }

    [Theory]
    [InlineData(TaskPriority.Low, 10)]
    [InlineData(TaskPriority.Medium, 20)]
    [InlineData(TaskPriority.High, 30)]
    public void RewardPoints_ShouldReflectPriority(TaskPriority priority, int expectedPoints)
    {
        // Arrange
        var task = CreateValidTask(priority: priority);

        // Act + Assert
        Assert.Equal(expectedPoints, task.RewardPoints);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidTitle_ShouldThrowDomainException(string invalidTitle)
    {
        Assert.Throws<DomainException>(() =>
            new TaskItem(invalidTitle, TaskCategory.School, TaskPriority.Low,
                         FutureDate, new TimeOnly(14, 0), ValidChildId, ValidCreatorId));
    }

    [Fact]
    public void Constructor_WithEmptyAssignedToId_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new TaskItem("Título válido", TaskCategory.School, TaskPriority.Low,
                         FutureDate, new TimeOnly(14, 0), Guid.Empty, ValidCreatorId));
    }

    [Fact]
    public void Constructor_WithEmptyCreatedById_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new TaskItem("Título válido", TaskCategory.School, TaskPriority.Low,
                         FutureDate, new TimeOnly(14, 0), ValidChildId, Guid.Empty));
    }

    [Fact]
    public void Constructor_WithPastDate_ShouldThrowDomainException()
    {
        // Arrange: ontem
        var pastDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        // Act + Assert
        Assert.Throws<DomainException>(() =>
            new TaskItem("Título válido", TaskCategory.School, TaskPriority.Low,
                         pastDate, new TimeOnly(14, 0), ValidChildId, ValidCreatorId));
    }

    [Fact]
    public void Start_FromPending_ShouldTransitionToInProgress()
    {
        var task = CreateValidTask();

        task.Start();

        Assert.Equal(TaskStatus.InProgress, task.Status);
    }

    [Fact]
    public void Start_FromInProgress_ShouldThrowDomainException()
    {
        var task = CreateValidTask();
        task.Start();

        Assert.Throws<DomainException>(() => task.Start());
    }

    [Fact]
    public void Complete_FromPending_ShouldTransitionToCompleted()
    {
        var task = CreateValidTask();

        task.Complete();

        Assert.Equal(TaskStatus.Completed, task.Status);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldThrowDomainException()
    {
        var task = CreateValidTask();
        task.Complete();

        Assert.Throws<DomainException>(() => task.Complete());
    }

    [Fact]
    public void Complete_FromSkipped_ShouldThrowDomainException()
    {
        var task = CreateValidTask();
        task.Skip();

        Assert.Throws<DomainException>(() => task.Complete());
    }

    [Fact]
    public void Skip_FromPending_ShouldTransitionToSkipped()
    {
        var task = CreateValidTask();

        task.Skip();

        Assert.Equal(TaskStatus.Skipped, task.Status);
    }

    [Fact]
    public void Skip_WhenAlreadyCompleted_ShouldThrowDomainException()
    {
        var task = CreateValidTask();
        task.Complete();

        Assert.Throws<DomainException>(() => task.Skip());
    }

    /// <summary>
    /// Teste de integração leve entre agregados: ao concluir, o fluxo de pontos
    /// (orquestrado pela camada de aplicação) credita corretamente à criança.
    /// </summary>
    [Fact]
    public void CompleteFlow_ShouldCreditRewardPointsToChild()
    {
        // Arrange
        var child = new Child("João", new DateOnly(2015, 3, 15));
        var task = CreateValidTask(priority: TaskPriority.High); // vale 30 pontos

        // Act: orquestração que a camada de Application fará
        task.Complete();
        child.AddPoints(task.RewardPoints);

        // Assert
        Assert.Equal(30, child.Points);
        Assert.Equal(TaskStatus.Completed, task.Status);
    }
}
