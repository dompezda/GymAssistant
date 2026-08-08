import { useEffect, useState, type ReactNode } from "react";
import {
    AuthContext,
    type AuthContextValue,
    type AuthStatus,
    type AuthUser,
} from "./authContext";

type LoginResponse = {
    accessToken: string;
    expiresAtUtc: string;
};
type AuthProviderProps = {
    children: ReactNode;
};

export default function AuthProvider({ children }: AuthProviderProps) {

const [status, setStatus] = useState<AuthStatus>("loading");
const [user, setUser] = useState<AuthUser | null>(null);

const authContextValue: AuthContextValue = {
    status,
    user,
    login: login,
    logout: logout,
};
useEffect(() => {
    console.log("Auth state changed:", status, user);
}, [status, user]);


    function logout() : void
    {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("expiresAtUtc");
        setUser(null);
        setStatus("unauthenticated");
    }

    async function login(email: string, password: string):Promise<void> {
    try {
    const response = await fetch("http://localhost:5235/api/auth/login", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({
            email: email,
            password: password,
        }),
    });        
    if (response.status === 401) {
        throw new Error("Nieprawidłowy email lub hasło");
    }

    if (!response.ok) {
        throw new Error("Wystąpił błąd. Spróbuj ponownie.");
    }

    const data : LoginResponse = await response.json();
    localStorage.setItem("accessToken", data.accessToken);
    localStorage.setItem("expiresAtUtc", data.expiresAtUtc);


    const authMeRequest = await fetch("http://localhost:5235/api/auth/me", {
        headers: {
            Authorization: `Bearer ${data.accessToken}`,
        },
    });
    if(!authMeRequest.ok)
    {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("expiresAtUtc");
        throw new Error("Wystąpił błąd. Spróbuj ponownie.");
    } 
    const userData: AuthUser = await authMeRequest.json();
    setUser(userData);
    setStatus("authenticated");
}
catch(err) {
    setStatus("unauthenticated");
    setUser(null);
    localStorage.removeItem("accessToken");
    localStorage.removeItem("expiresAtUtc");
    throw err;
}}




useEffect(() => {

    async function checkAuthentication() {
        const accessToken = localStorage.getItem("accessToken");
        if(!accessToken) {
            setStatus("unauthenticated");
            return;
        };
        try
        {
            const response = await fetch("http://localhost:5235/api/auth/me", {
            headers: {
                Authorization: `Bearer ${accessToken}`,
                },
            });

            if (!response.ok) {
                localStorage.removeItem("accessToken");
                localStorage.removeItem("expiresAtUtc");
                setUser(null);
                setStatus("unauthenticated");
                return;
            }

            const userData: AuthUser = await response.json();
            setUser(userData);
            setStatus("authenticated");
        }
        catch
        {
            setUser(null);
            setStatus("unauthenticated");
        }      
    }
    checkAuthentication();
},[]);

return (
    <AuthContext.Provider value={authContextValue}>
        {children}
    </AuthContext.Provider>
);
}

