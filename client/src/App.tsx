import { Route, Routes } from "react-router-dom";
import { motion } from "framer-motion";
import { useTheme } from "./stores/theme";

const CATEGORIES = ["Shoes", "Handbags", "Cosmetics", "Jewelry", "Accessories"];

function ThemeToggle() {
  const { theme, toggle } = useTheme();
  return (
    <button
      onClick={toggle}
      aria-label="Toggle dark mode"
      className="rounded-full border border-zinc-200 p-2 text-lg transition hover:border-brand-400 dark:border-zinc-700"
    >
      {theme === "dark" ? "🌞" : "🌙"}
    </button>
  );
}

function Landing() {
  return (
    <main className="mx-auto flex max-w-4xl flex-col items-center px-6 pt-24 pb-16 text-center">
      <motion.div
        initial={{ opacity: 0, y: 24 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.6 }}
      >
        <span className="rounded-full bg-brand-50 px-4 py-1.5 text-sm font-semibold text-brand-600 dark:bg-brand-600/10 dark:text-brand-400">
          Launching soon
        </span>
        <h1 className="mt-6 text-5xl font-extrabold tracking-tight sm:text-6xl">
          Style, delivered.
          <span className="block bg-gradient-to-r from-brand-500 to-accent-500 bg-clip-text text-transparent">
            M1 Stores
          </span>
        </h1>
        <p className="mx-auto mt-6 max-w-xl text-lg text-zinc-600 dark:text-zinc-400">
          Your marketplace for shoes, handbags, cosmetics, jewelry and
          accessories — with M-Pesa &amp; card checkout, order tracking and
          fast delivery.
        </p>
      </motion.div>

      <motion.ul
        initial="hidden"
        animate="visible"
        variants={{ visible: { transition: { staggerChildren: 0.08 } } }}
        className="mt-10 flex flex-wrap justify-center gap-3"
      >
        {CATEGORIES.map((category) => (
          <motion.li
            key={category}
            variants={{
              hidden: { opacity: 0, scale: 0.9 },
              visible: { opacity: 1, scale: 1 },
            }}
            className="rounded-full border border-zinc-200 px-5 py-2 text-sm font-medium text-zinc-700 dark:border-zinc-700 dark:text-zinc-300"
          >
            {category}
          </motion.li>
        ))}
      </motion.ul>
    </main>
  );
}

export default function App() {
  return (
    <div className="min-h-screen bg-white text-zinc-900 transition-colors dark:bg-zinc-950 dark:text-zinc-50">
      <header className="mx-auto flex max-w-6xl items-center justify-between px-6 py-5">
        <a href="/" className="flex items-center gap-2 text-xl font-extrabold">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-accent-500 text-sm font-black text-white">
            M1
          </span>
          M1 Stores
        </a>
        <ThemeToggle />
      </header>

      <Routes>
        <Route path="*" element={<Landing />} />
      </Routes>

      <footer className="mx-auto max-w-6xl px-6 py-10 text-center text-sm text-zinc-500">
        © 2026 M1 Stores · Built by Mary Wainaina · WayneTech Studio
      </footer>
    </div>
  );
}
