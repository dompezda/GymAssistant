import { useState } from "react";
import { Link } from "react-router-dom";
import { getApiUrl } from "../../config/apiConfig";

export default function ForgotPasswordPage()
{
    const [email,setEmail] = useState("");
    const [error,setError] = useState("");
    const [info,setInfo] = useState<string>();
    const apiUrl = getApiUrl();
    async function handleReset(): Promise<void> {
         setError("");
        if(!email){
            setError("Uzupełnij pole email");
            return;
        }
         try { 
            const response = await fetch(`${apiUrl}/api/auth/forgot-password`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    Email: email,
                }),
            });

            if(response.ok) {
                setInfo("Jeśli konto o podanym adresie istnieje, wysłaliśmy wiadomość z instrukcją resetowania hasła");
            }
            if (!response.ok) {
                setError("Nie udało się wysłać żądania. Spróbuj ponownie.");
                return;
            }
        } catch {
            setError("Nie udało się połączyć z serwerem. Spróbuj ponownie.");
        }
    }

    return (
        <div className="auth-page">
          <div className="stack">
            <h1 className="page-title">Reset hasła</h1>
          </div>
            <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" placeholder="Email" />
            {error && (
                <div className="form-error">
                    {error}
                </div>
            )}
            {info && (
                <div>
                    {info}
                </div>
            )}
            <button className="btn btn-primary" onClick={handleReset}>
                Zresetuj hasło
            </button>

        <div className="auth-switch">
            Masz już konto? <Link to="/login">Zaloguj się</Link>
        </div>
        </div>
    )
}