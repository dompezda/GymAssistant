import { useRef, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { getApiUrl } from "../../config/apiConfig";

export default function NewPasswordPage()
{
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error,setError] = useState("");
    const [searchParams] = useSearchParams();
    const [isSetting,setIsSetting] = useState<boolean>(false);
    const isSettingRef = useRef<boolean>(false);
    const apiUrl = getApiUrl();
    const navigate = useNavigate();


    async function handleReset(
    event: React.FormEvent<HTMLFormElement>
): Promise<void> {
    event.preventDefault();

    if(isSettingRef.current) return;
        setError("");
        if(!password || !confirmPassword)
        {
            setError("Uzupełnij dane");
            return;
        }
        if(password !== confirmPassword)
        {
            setError("Hasła nie są identyczne");
            return;
        }
        const userId = searchParams.get("userId");
        const token = searchParams.get("token");
        if(!userId || !token)
        {
            setError("Niepoprawny request");
            return;
        }

        isSettingRef.current=true;
        setIsSetting(true);
        try
        {
            const response = await fetch(`${apiUrl}/api/auth/reset-password`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    UserId: userId,
                    Token: token,
                    Password: password
                }),
            });
            if (!response.ok) {
                setError("Nie udało się zresetować hasła.");
                return;
            }

            navigate("/login");

        } catch {
            setError("Nie udało się połączyć z serwerem. Spróbuj ponownie.");
        }
        finally
        {
            setIsSetting(false);
            isSettingRef.current=false;
        }
    }

    return(
    <div className="auth-page">
        <form onSubmit={handleReset} className="stack">
            <h1 className="page-title">Zmiana hasła</h1>

            <input required autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} type="password" placeholder="Hasło" />
            <input required autoComplete="new-password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} type="password" placeholder="Powtórz hasło" />
            {error && (
                <div className="form-error">
                    {error}
                </div> 
            )}
            <div className="password-requirements">
                <p>Hasło musi zawierać:</p>
                <ul>
                    <li>Minimum 8 znaków</li>
                    <li>Małą i wielką literę</li>
                    <li>Co najmniej jedną cyfrę</li>
                    <li>Co najmniej jeden znak specjalny</li>
                </ul>
            </div>
            <button
                type="submit"
                className="btn btn-primary"
                disabled={isSetting}
            >
                {isSetting ? "Zapisywanie..." : "Ustaw nowe hasło"}
            </button>
            <div className="auth-switch">
                Masz już konto? <Link to="/login">Zaloguj się</Link>
            </div>
        </form>
</div>
    )
}