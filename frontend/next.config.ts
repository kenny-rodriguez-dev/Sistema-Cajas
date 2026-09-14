import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  output: 'standalone',
  // Le dice a Next.js que al compilar arme también un servidor
  // autocontenido en .next/standalone. Es el modo pensado para Docker.
};

export default nextConfig;