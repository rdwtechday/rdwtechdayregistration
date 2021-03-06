﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RdwTechdayRegistration.Services
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string plainMessage, string htmlMessage);
    }
}
