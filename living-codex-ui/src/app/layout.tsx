import type { Metadata } from 'next';
import { Inter } from 'next/font/google';
import './globals.css';
import { Providers } from './providers';
import { GlobalControls } from '@/components/controls/GlobalControls';
import { Navigation } from '@/components/ui/Navigation';
import { cn } from '@/lib/utils';

const inter = Inter({ subsets: ['latin'] });

export const metadata: Metadata = {
  title: 'Living Codex - Find ideas that resonate',
  description: 'Explore concepts, people, and moments connected by resonance. Everything is a Node.',
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className="dark">
      <body className={cn('min-h-screen bg-page text-foreground', inter.className)}>
        <Providers>
          <header className="bg-card border-b border-card">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
              <div className="flex justify-between items-center h-16">
                <div className="text-xl font-bold text-high-contrast">Living Codex</div>
                <Navigation />
              </div>
            </div>
          </header>
          {children}
          <GlobalControls />
        </Providers>
      </body>
    </html>
  );
}
