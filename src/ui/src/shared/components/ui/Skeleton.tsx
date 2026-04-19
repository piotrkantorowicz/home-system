import { cn } from '@shared/lib/utils';

// eslint-disable-next-line @typescript-eslint/no-empty-object-type
export type SkeletonProps = React.HTMLAttributes<HTMLDivElement>;

export function Skeleton({ className, ...props }: SkeletonProps) {
  return <div className={cn('bg-muted animate-pulse rounded-md', className)} {...props} />;
}
