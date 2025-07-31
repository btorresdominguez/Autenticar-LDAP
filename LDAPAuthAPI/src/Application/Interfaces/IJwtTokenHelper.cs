using Application.DTOs;
using Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IJwtTokenHelper
    {
        string GenerateToken(UserInfoDto userInfo);
    }
}
