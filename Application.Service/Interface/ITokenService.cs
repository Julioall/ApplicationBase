﻿using Application.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Service.Interface
{
    public interface ITokenService
    {
        Task<string> GenerateToken(LoginDto loginDto);
    }
}
