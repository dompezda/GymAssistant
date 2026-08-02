import { useEffect, useState, type ReactNode } from "react";
import {
    AuthContext,
    type AuthContextValue,
    type AuthStatus,
    type AuthUser,
} from "./authContext";

type AuthProviderProps = {
    children: ReactNode;
};

export default function AuthProvider({ children }: AuthProviderProps) {

const [status, setStatus] = useState<AuthStatus>("loading");
const [user, setUser] = useState<AuthUser | null>(null);

const authContextValue: AuthContextValue = {
    status,
    user,
};
useEffect(() => {
    console.log("Auth state changed:", status, user);
}, [status, user]);


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


