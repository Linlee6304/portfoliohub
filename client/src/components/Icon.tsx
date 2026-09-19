import type { CSSProperties } from "react";
export type IconName =
  | "home"
  | "grid"
  | "user"
  | "menu"
  | "chevron"
  | "lock"
  | "logout"
  | "arrow";
const paths: Record<IconName, string> = {
  home: "m3 10 9-7 9 7v10a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1z",
  grid: "M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z",
  user: "M20 21v-2a6 6 0 0 0-6-6h-4a6 6 0 0 0-6 6v2 M16 6a4 4 0 1 1-8 0 4 4 0 0 1 8 0",
  menu: "M4 6h16 M4 12h16 M4 18h16",
  chevron: "m6 9 6 6 6-6",
  lock: "M5 10h14v11H5z M8 10V6a4 4 0 0 1 8 0v4",
  logout: "M9 21H4V3h5 M10 12h11 m-4-4 4 4-4 4",
  arrow: "M5 12h14 m-6-6 6 6-6 6",
};
export default function Icon({
  name,
  style,
}: {
  name: IconName;
  style?: CSSProperties;
}) {
  return (
    <svg
      className="icon"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
      style={style}
    >
      <path d={paths[name]} />
    </svg>
  );
}
