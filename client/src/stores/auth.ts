import { create } from "zustand";
import { persist } from "zustand/middleware";
import type { AuthResponse, User } from "../api/types";

interface AuthState {
  user: User | null;
  accessToken: string | null;
  refreshToken: string | null;
  setAuth: (auth: AuthResponse) => void;
  setUser: (user: User) => void;
  logout: () => void;
}

export const useAuth = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      setAuth: (auth) =>
        set({ user: auth.user, accessToken: auth.accessToken, refreshToken: auth.refreshToken }),
      setUser: (user) => set({ user }),
      logout: () => set({ user: null, accessToken: null, refreshToken: null }),
    }),
    { name: "m1-auth" },
  ),
);
