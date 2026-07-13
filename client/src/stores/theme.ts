import { create } from "zustand";
import { persist } from "zustand/middleware";

type Theme = "light" | "dark";

interface ThemeState {
  theme: Theme;
  toggle: () => void;
}

const systemPrefersDark = () =>
  window.matchMedia?.("(prefers-color-scheme: dark)").matches ?? false;

const apply = (theme: Theme) =>
  document.documentElement.setAttribute("data-theme", theme);

export const useTheme = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme: systemPrefersDark() ? "dark" : "light",
      toggle: () => {
        const next: Theme = get().theme === "dark" ? "light" : "dark";
        apply(next);
        set({ theme: next });
      },
    }),
    {
      name: "m1-theme",
      onRehydrateStorage: () => (state) => {
        if (state) apply(state.theme);
      },
    },
  ),
);

// Apply on first load (before rehydration completes for first-time visitors).
apply(useTheme.getState().theme);
