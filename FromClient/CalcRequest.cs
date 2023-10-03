using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetUtils.ToClient;

namespace DemoServer.FromClient {
    public class CalcRequest : NetUtils.ToServer.CalcRequest {
        private bool Calculate(ref int i) {
            var a = double.Parse(Operations[i - 1].Replace('.', ','));
            var b = double.Parse(Operations[i + 1].Replace('.', ','));
            switch (Operations[i][0]) {
                case '*':
                    Operations[i - 1] = (a * b).ToString(CultureInfo.CurrentCulture);
                    break;
                case '/':
                    if (b == 0) {
                        return false;
                    }

                    Operations[i - 1] = (a / b).ToString(CultureInfo.CurrentCulture);
                    break;
                case '+':
                    Operations[i - 1] = (a + b).ToString(CultureInfo.CurrentCulture);
                    break;
                case '-':
                    Operations[i - 1] = (a - b).ToString(CultureInfo.CurrentCulture);
                    break;
            }

            Operations.RemoveAt(i);
            Operations.RemoveAt(i);
            i--;
            return true;
        }

        public override async Task Handle() {
            if (Operations.Count == 0) {
                await Net!.Send(new CalcResponse {
                    Error = CalcError.InvalidInput
                });
                return;
            }

            switch (Operations[^1][0]) {
                case '+':
                case '-':
                case '*':
                case '/':
                    Operations.RemoveAt(Operations.Count - 1);
                    break;
            }

            for (var i = 0; i < Operations.Count; i++)
                switch (Operations[i][0]) {
                    case '*':
                        Calculate(ref i);
                        break;
                    case '/':
                        if (!Calculate(ref i)) {
                            await Net!.Send(new CalcResponse {
                                Error = CalcError.DivByZero
                            });
                            return;
                        }

                        break;
                }

            for (var i = 0; i < Operations.Count; i++)
                switch (Operations[i][0]) {
                    case '+':
                    case '-':
                        Calculate(ref i);
                        break;
                }

            await Net!.Send(new CalcResponse {
                Result = double.Parse(Operations[0])
            });
        }
    }
}
