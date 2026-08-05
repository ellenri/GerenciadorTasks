using System;
using System.Collections.Generic;
using System.Text;

namespace GerenciadorTasks.Core.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
