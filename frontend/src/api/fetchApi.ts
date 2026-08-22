import { getApiUrl } from "../config/apiConfig";

type TokenResponse = {
    accessToken: string;
    expiresAtUtc: string;
};


const apiUrl = getApiUrl();
let refreshPromise: Promise<string | null> | null = null;
export const fetchApi = async (path: string, options: RequestInit = {}) => {
    const requestUrl = `${apiUrl}${path}`;
    const token = localStorage.getItem("accessToken");
    const headers = new Headers(options.headers);
    if (token) {
        headers.set("Authorization", `Bearer ${token}`);
    }
    const response = await fetch(requestUrl, { ...options, headers });
    if(response.status !== 401) {
        return response;
    }

    const currentToken = localStorage.getItem("accessToken");

    if (currentToken && currentToken !== token) {
        headers.set("Authorization", `Bearer ${currentToken}`);
        return fetch(requestUrl, { ...options, headers });
    }
    if (!refreshPromise) {
        refreshPromise = refreshAccessToken()
            .finally(() => {
                refreshPromise = null;
            });
    }

    const newJwtToken = await refreshPromise;
    if(!newJwtToken) {
        return response;
    }
    headers.set("Authorization", `Bearer ${newJwtToken}`);

    return fetch(requestUrl, { ...options, headers });
};

export const refreshAccessToken = async(): Promise<string | null> => {
    const response = await fetch(`${apiUrl}/api/auth/refresh`, {
        method: "POST",
        credentials: "include",
    });   
    if(!response.ok) {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("expiresAtUtc");
        return null;
    }
    const data: TokenResponse = await response.json();
    localStorage.setItem("accessToken", data.accessToken);
    localStorage.setItem("expiresAtUtc", data.expiresAtUtc);
    return data.accessToken;
}