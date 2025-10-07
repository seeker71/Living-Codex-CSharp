import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  /* config options here */
  eslint: {
    // Ensure ESLint errors are caught during builds
    ignoreDuringBuilds: false,
  },
};

export default nextConfig;
