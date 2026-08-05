using GerenciadorTasks.Core.Entities;
using GerenciadorTasks.Core.Exceptions;

namespace GerenciadorTasks.UnitTests.Entities;

/// <summary>
/// Testes da entidade Child (criança).
/// Cada teste segue o padrão AAA: Arrange (preparar) -> Act (agir) -> Assert (verificar).
/// </summary>
public class ChildTests
{
    // [Fact] = um único cenário, sem parâmetros.
    [Fact]
    public void Constructor_WithValidData_ShouldCreateChildWithZeroPoints()
    {
        // Arrange
        var name = "João Silva";
        var birthDate = new DateOnly(2015, 3, 15);

        // Act
        var child = new Child(name, birthDate);

        // Assert
        Assert.Equal("João Silva", child.FullName);
        Assert.Equal(birthDate, child.BirthDate);
        Assert.Equal(0, child.Points);            // nova criança começa com 0 pontos
        Assert.NotEqual(Guid.Empty, child.Id);    // ganhou um Id ao nascer
    }

    // [Theory] = mesmo teste executado várias vezes com dados diferentes (InlineData).
    [Theory]
    [InlineData("")]        // string vazia
    [InlineData("   ")]     // só espaços
    public void Constructor_WithInvalidName_ShouldThrowDomainException(string invalidName)
    {
        // Arrange
        var birthDate = new DateOnly(2015, 3, 15);

        // Act: capturamos a ação numa lambda para poder verificar a exceção
        var act = () => new Child(invalidName, birthDate);

        // Assert
        var ex = Assert.Throws<DomainException>(act);
        Assert.Contains("nome", ex.Message.ToLower());
    }

    [Fact]
    public void Constructor_WithFutureBirthDate_ShouldThrowDomainException()
    {
        // Arrange: data de amanhã
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);

        // Act
        var act = () => new Child("Maria", futureDate);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void AddPoints_WithPositiveAmount_ShouldAccumulate()
    {
        // Arrange
        var child = new Child("Pedro", new DateOnly(2016, 1, 1));

        // Act: ganha 10 e depois 20 pontos
        child.AddPoints(10);
        child.AddPoints(20);

        // Assert
        Assert.Equal(30, child.Points);
    }

    [Fact]
    public void AddPoints_WithNegativeAmount_ShouldThrowDomainException()
    {
        // Arrange
        var child = new Child("Pedro", new DateOnly(2016, 1, 1));

        // Act + Assert
        Assert.Throws<DomainException>(() => child.AddPoints(-5));
    }

    [Fact]
    public void AddPoints_WithZero_ShouldKeepPointsUnchanged()
    {
        // Arrange
        var child = new Child("Ana", new DateOnly(2017, 5, 10));
        child.AddPoints(15);

        // Act
        child.AddPoints(0);

        // Assert
        Assert.Equal(15, child.Points);
    }

    [Fact]
    public void DeductPoints_WithValidAmount_ShouldDecreasePoints()
    {
        // Arrange
        var child = new Child("João", new DateOnly(2015, 3, 15));
        child.AddPoints(30);

        // Act
        child.DeductPoints(12);

        // Assert
        Assert.Equal(18, child.Points);
    }

    [Fact]
    public void DeductPoints_ToZero_ShouldBeAllowed()
    {
        // Arrange
        var child = new Child("João", new DateOnly(2015, 3, 15));
        child.AddPoints(20);

        // Act: zera o saldo — limite inferior permitido
        child.DeductPoints(20);

        // Assert
        Assert.Equal(0, child.Points);
    }

    [Fact]
    public void DeductPoints_ExceedingBalance_ShouldThrowDomainException()
    {
        // Arrange
        var child = new Child("João", new DateOnly(2015, 3, 15));
        child.AddPoints(10);

        // Act + Assert: tentar descontar mais do que tem não pode zerar negativamente
        var ex = Assert.Throws<DomainException>(() => child.DeductPoints(25));
        Assert.Contains("insuficiente", ex.Message.ToLower());
        Assert.Equal(10, child.Points); // saldo intacto (consistência)
    }

    [Fact]
    public void DeductPoints_WithNegativeAmount_ShouldThrowDomainException()
    {
        // Arrange
        var child = new Child("João", new DateOnly(2015, 3, 15));
        child.AddPoints(10);

        // Act + Assert
        Assert.Throws<DomainException>(() => child.DeductPoints(-5));
    }

    [Fact]
    public void Rename_WithValidName_ShouldUpdateFullName()
    {
        // Arrange
        var child = new Child("João", new DateOnly(2015, 3, 15));

        // Act
        child.Rename("João Pedro Silva");

        // Assert
        Assert.Equal("João Pedro Silva", child.FullName);
    }

    [Fact]
    public void Rename_WithEmptyName_ShouldThrowDomainException()
    {
        // Arrange
        var child = new Child("João", new DateOnly(2015, 3, 15));

        // Act + Assert
        Assert.Throws<DomainException>(() => child.Rename(""));
    }
}
