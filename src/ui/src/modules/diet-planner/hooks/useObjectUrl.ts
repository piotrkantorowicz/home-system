import { useEffect, useState } from 'react';

export function useObjectUrl(blob: Blob | null | undefined) {
  const [resource, setResource] = useState<{ blob: Blob; url: string } | null>(null);
  useEffect(() => {
    if (!blob) return;
    const url = URL.createObjectURL(blob);
    // eslint-disable-next-line react-hooks/set-state-in-effect -- REASON: Publish a DOM resource created outside render, paired with revocation.
    setResource({ blob, url });
    return () => {
      URL.revokeObjectURL(url);
    };
  }, [blob]);
  return resource?.blob === blob ? resource?.url : undefined;
}
