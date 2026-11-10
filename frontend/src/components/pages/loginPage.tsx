import { useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import useAuth from "../../auth/useAuth";

export default function LoginPage() {

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");
    const { login } = useAuth();
    const [isLogging,setIsLogging] = useState<boolean>(false);
    const isLoggingRef = useRef<boolean>(false);

    const navigate = useNavigate();


    async function handleLogin(): Promise<void> {
    if (isLoggingRef.current) return;

    isLoggingRef.current = true;
    setIsLogging(true);
    setError("");
        try {

            await login(email, password)
            navigate("/main-menu");
        } catch (err) {
            if (err instanceof TypeError) {
                setError("Nie udało się połączyć z serwerem. Spróbuj ponownie.");
                return;
            }
            setError((err as Error).message);
        }
        finally
        {
            setIsLogging(false);
            isLoggingRef.current=false;
        }
    }

    return (

        <div className="auth-page">
            <h1 className="page-title">Logowanie</h1>
            <form
                className="modal-body"
                onSubmit={(e) => {
                    e.preventDefault();
                    void handleLogin();
                }}
            >
                <input
                    type="email"
                    autoComplete="email"
                    required
                    placeholder="Email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                />
                <input
                    type="password"
                    required
                    autoComplete="current-password"
                    placeholder="Hasło"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                />
                {error && <p>{error}</p>}
                <button className="btn btn-primary" disabled={isLogging}>
                    {isLogging ? "Logowanie..." : "Zaloguj się"}
                </button>
                <div className="auth-switch">
                    Nie masz konta? <Link to="/register">Zarejestruj się</Link>
                </div>
                <div className="auth-switch">
                    <Link to="/forgot-password">Zapomniałeś hasła?</Link>
                </div>
            </form>
        </div>
    )
}