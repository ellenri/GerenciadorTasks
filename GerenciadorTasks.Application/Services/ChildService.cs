using GerenciadorTasks.Application.Abstractions;
using GerenciadorTasks.Application.Dtos;
using GerenciadorTasks.Core.Entities;

namespace GerenciadorTasks.Application.Services;

/// <summary>Casos de uso relacionados a crianças (consulta).</summary>
public class ChildService
{
    private readonly IChildRepository _children;
    private readonly IUnitOfWork _unitOfWork;

    public ChildService(IChildRepository children, IUnitOfWork unitOfWork)
    {
        _children = children;
        _unitOfWork = unitOfWork;
    }

    /// Cadastra uma nova criança (com avatar escolhido no formulário).
    public async Task<ChildResponse> CreateAsync(CreateChildRequest request, CancellationToken ct)
    {
        var birthDate = DateOnly.Parse(request.BirthDate);
        var child = new Child(request.Name, birthDate, request.Avatar);
        await _children.AddAsync(child, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return ChildResponse.From(child);
    }

    public async Task<IReadOnlyList<ChildResponse>> GetAllAsync(CancellationToken ct)
    {
        var children = await _children.ListAsync(ct);
        return children.Select(ChildResponse.From).ToList();
    }

    public async Task<ChildResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var child = await _children.GetByIdAsync(id, ct);
        return child is null ? null : ChildResponse.From(child);
    }
}
