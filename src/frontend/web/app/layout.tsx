import type { Metadata } from "next";
import { AppearanceProvider } from "@/components/theme/appearance-provider";
import { getAppearanceBootScript } from "@/lib/appearance";
import "./globals.css";

export const metadata: Metadata = {
  title: "Car Rental ERP Console",
  description: "Secure operations console for fleet, bookings, rentals, owners, and payments.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body suppressHydrationWarning>
        <script dangerouslySetInnerHTML={{ __html: getAppearanceBootScript() }} />
        <AppearanceProvider>{children}</AppearanceProvider>
      </body>
    </html>
  );
}
