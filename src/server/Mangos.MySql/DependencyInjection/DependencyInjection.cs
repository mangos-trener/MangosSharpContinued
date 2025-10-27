//
// Copyright (C) 2013-2025 getMaNGOS <https://www.getmangos.eu>
//
// This program is free software. You can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation. either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY. Without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//

using Mangos.Common.Legacy.Databases;
using Mangos.MySql.GetAccountInfo;
using Mangos.MySql.GetRealmList;
using Mangos.MySql.IsBannedAccount;
using Mangos.MySql.UpdateAccount;
using Microsoft.Extensions.DependencyInjection;

namespace Mangos.MySql.DependencyInjection;
public static class DependencyInjection
{
    public static IServiceCollection AddDatabase(this IServiceCollection services)
    {
        services.AddSingleton<ConnectionFactory>();
        services.AddSingleton<AccountDatabase>();
        services.AddSingleton<CharacterDatabase>();
        services.AddSingleton<WorldDatabase>();

        services.AddSingleton(provider =>
        {
            var factory = provider.GetRequiredService<ConnectionFactory>();
            return factory.ConnectToAccountDataBase();
        });

        services.AddSingleton<IGetAccountInfoQuery, GetAccountInfoQuery>();
        services.AddSingleton<IIsBannedAccountQuery, IsBannedAccountQuery>();
        services.AddSingleton<IUpdateAccountCommand, UpdateAccountCommand>();
        services.AddSingleton<IGetRealmListQuery, GetRealmListQuery>();

        return services;
    }
}
