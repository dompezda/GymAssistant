import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { getApiUrl } from "../../config/apiConfig";

export default function NewPasswordPage()
{
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error,setError] = useState("");
    const [searchParams] = useSearchParams();
    
    const apiUrl = getApiUrl();
    const navigate = useNavigate();


    async function handleReset(): Promise<void> {
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
    }

    return(
    <div className="auth-page">
        <div className="stack">
            <h1 className="page-title">Zmiana hasła</h1>

            <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" placeholder="Hasło" />
            <input value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} type="password" placeholder="Powtórz hasło" />
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
            <button className="btn btn-primary" onClick={handleReset}>
                Ustaw nowe hasło
            </button>
            <div className="auth-switch">
                Masz już konto? <Link to="/login">Zaloguj się</Link>
            </div>
        </div>
</div>
    )
}