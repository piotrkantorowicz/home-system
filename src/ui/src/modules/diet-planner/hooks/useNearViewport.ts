import { useEffect, useRef, useState } from 'react';

/** Latches to `true` once the element comes within 200px of the viewport, and stays there. */
export function useNearViewport() {
  const ref = useRef<HTMLDivElement>(null);
  // Without IntersectionObserver there is nothing to wait for — treat every element as near.
  const [near, setNear] = useState(() => typeof IntersectionObserver === 'undefined');
  useEffect(() => {
    const element = ref.current;
    if (!element || near) return;
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (!entry?.isIntersecting) return;
        setNear(true);
        observer.disconnect();
      },
      { rootMargin: '200px' },
    );
    observer.observe(element);
    return () => {
      observer.disconnect();
    };
  }, [near]);
  return [ref, near] as const;
}
