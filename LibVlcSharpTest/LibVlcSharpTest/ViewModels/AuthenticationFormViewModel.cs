using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LibVlcSharpTest.ViewModels
{
    public class AuthenticationFormViewModel
    {
        public string Title { get; set; }
        public string Text { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Store { get; set; }

        public AuthenticationFormViewModel()
        {
            Store = true;
        }

        public Task<bool> Validate()
        {
            var result = !string.IsNullOrWhiteSpace(Username);

            return Task.FromResult(result);
        }
    }
}
