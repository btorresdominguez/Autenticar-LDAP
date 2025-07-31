using Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class AuthResponseDto
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public UserInfoDto User { get; set; }

        public string Token { get; set; }

    }

}
