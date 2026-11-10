import { useRef, useState } from "react";
import { Link } from "react-router-dom";
import { getApiUrl } from "../../config/apiConfig";

export default function ForgotPasswordPage()
{
    const [email,setEmail] = useState("");
    const [error,setError] = useState("");
    const [info,setInfo] = useState<string>();
    const [isSubmitting,setIsSubmitting] = useState<boolean>(false);
    const isSubmittingRef = useRef<boolean>(false);
    const apiUrl = getApiUrl();
    async function handleReset(
    event: React.FormEvent<HTMLFormElement>
        ): Promise<void> {
            event.preventDefault();
        if(isSubmittingRef.current==true) return;
         setError("");
         setInfo(undefined);
        if(!email){
            setError("Uzupełnij pole email");
            return;
        }
        isSubmittingRef.current = true; 
        setIsSubmitting(true);
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
        finally
        {
            setIsSubmitting(false);
            isSubmittingRef.current=false;
        }
    }

    return (
        <div className="auth-page">
          <div className="stack">
            <h1 className="page-title">Reset hasła</h1>
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
          <form onSubmit={handleReset}>
            <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" placeholder="Email" />
            
            <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                Zresetuj hasło
            </button>
            </form>
            </div>
            
        <div className="auth-switch">
            Masz już konto? <Link to="/login">Zaloguj się</Link>
        </div>
        </div>
    )
}