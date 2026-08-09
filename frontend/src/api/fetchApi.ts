export const fetchApi = async (url: string, options: RequestInit = {}) => {
    const token = localStorage.getItem("accessToken");
    const headers = new Headers(options.headers);
    if (token) {
        headers.set("Authorization", `Bearer ${token}`);
    }
    return fetch(url, { ...options, headers });
};