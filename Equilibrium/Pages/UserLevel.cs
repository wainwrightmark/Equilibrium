using System.Text;

namespace Equilibrium.Pages;

public sealed record UserLevel(string Name, Level Level, bool IsBeaten, int? FunStars, int? DifficultyStars)
{
    public string GetDisplayString()
    {
        var sb = new StringBuilder();

        sb.Append(Name);
        sb.AppendLine();
        //sb.Append(' ');
        sb.Append(IsBeaten ? '✅' : '❎');
        sb.Append(' ');
        AppendStars(FunStars, 5, sb);
        sb.Append(' ');
        AppendStars(DifficultyStars,5, sb);


        static void AppendStars(int? s, int max, StringBuilder sb)
        {
            for (var i = 0; i < (s??0); i++)
            {
                sb.Append('★');
            }

            for (var i = (s??0); i < max; i++)
            {
                sb.Append('☆');
            }
        }

        return sb.ToString();
    }
}