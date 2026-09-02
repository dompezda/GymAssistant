import { useState } from "react";
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
    const { login } = useAuth();
    async function handleRegister() 
    {
        setError("");
        if(!email || !password || !confirmPassword) {
            setError("All fields are required.");
            return;
        }
        if(password !== confirmPassword) {
            setError("Passwords do not match.");
            return;
        }

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
                await login(email, password);
                navigate("/main-menu");
            } else {
                const errors: string[] = await response.json();
                setError(errors.join(`\n`));        
            }
        } catch (err) {
            setError((err as Error).message);
        }
    }
    return(

        
    <>
    <div className="page auth-page">
    <div className="card auth-card">
        <div className="stack">
            <h1>Register</h1>
            <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" placeholder="Email" />
            <input value={password} onChange={(e) => setPassword(e.target.value)} type="password" placeholder="Password" />
            <input value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} type="password" placeholder="Confirm Password" />
            {error && (
                <div className="form-error">
                    {error}
                </div>
            )}
            <button className="btn btn-primary" onClick={handleRegister}>
                Register
            </button>
            <div className="auth-switch">
                Masz już konto? <Link to="/login">Zaloguj się</Link>
            </div>
        </div>
    </div>
</div>
    
    
    
    
</>
)
}
