import { useRef, useState } from "react";
import { getApiUrl } from "../../config/apiConfig";
import useAuth from "../../auth/useAuth";
import { Link, useNavigate } from "react-router-dom";

export default function RegisterPage() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error, setError] = useState("");
    const apiUrl = getApiUrl();
    const navigate = useNavigate();
    const [isSubmitting,setIsSubmitting] = useState<boolean>(false);
    const [disableRegister, setDisableRegister] = useState<boolean>(false);
    const isSubmittingRef = useRef<boolean>(false);
    const { login } = useAuth();
    async function handleRegister(
        event: React.FormEvent<HTMLFormElement>
    ): Promise<void> {
    event.preventDefault();
    
        if(isSubmittingRef.current || disableRegister) return;

        setError("");

        if(!email || !password || !confirmPassword) {
            setError("Wszystkie pola są wymagane.");
            return;
        }
        if(password !== confirmPassword) {
            setError("Hasła nie są zgodne.");
            return;
        }
        isSubmittingRef.current= true;
        setIsSubmitting(true);
        try { 
            const response = await fetch(`${apiUrl}/api/auth/register`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    email: email,
                    password: password,
                }),
            });

            if(response.ok) {
                try {
                    await login(email, password);
                    navigate("/main-menu");
                } catch {
                    setDisableRegister(true);
                    setError(
                        "Konto zostało utworzone, ale automatyczne logowanie nie powiodło się. Zaloguj się ręcznie."
                    );
                }
            } else {
                const errors: string[] = await response.json();
                setError(errors.join(`\n`));        
            }
        } catch {
            setError("Nie udało się połączyć z serwerem. Spróbuj ponownie.");
        }
        finally
        {
            setIsSubmitting(false);
            isSubmittingRef.current = false;
        }
    }
    return(

        
    <>
    <div className="auth-page">
        <form className="stack" onSubmit={handleRegister}>
            <h1 className="page-title">Rejestracja</h1>
            <input required autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} type="email" placeholder="Email" />
            <input required autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} type="password" placeholder="Hasło" />
            <input required autoComplete="new-password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} type="password" placeholder="Powtórz hasło" />
            {error && (
                <div className="form-error">
                    {error}
                </div>
            )}
            <button className="btn btn-primary" disabled={isSubmitting || disableRegister} type="submit">
                {isSubmitting ? "Trwa operacja" : "Zarejestruj się"}
            </button>
            <div className="auth-switch">
                Masz już konto? <Link to="/login">Zaloguj się</Link>
            </div>
        </form>
</div>
    
    
    
    
</>
)
}
