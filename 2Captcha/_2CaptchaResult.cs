namespace _2Captcha
{
	public readonly struct _2CaptchaResult
    {
        public readonly bool Success;
        public readonly string Response;
        public readonly string SolveId;

        public _2CaptchaResult(bool success, string response, string solveId)
        {
            Success = success;
            Response = response;
            SolveId = solveId;
        }

    }
}
