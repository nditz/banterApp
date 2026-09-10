import { afterEach, describe, expect, it, vi } from "vitest";
import { hasAdvertisingConsent, readAdvertisingConsent, setAdvertisingConsent } from "./advertising-consent";
import {
  parseStandingsPayload,
  datasetStatusFromMatchweek,
  isUnofficialMatchweek,
} from "./football-dataset";
import { AD_PLACEMENT_KEYS, canRequestAds, resolveAdSlotId } from "./ads";

function stubStorage() {
  const store: Record<string, string> = {};
  vi.stubGlobal("window", {
    localStorage: {
      getItem: (key: string) => store[key] ?? null,
      setItem: (key: string, value: string) => {
        store[key] = value;
      },
      removeItem: (key: string) => {
        delete store[key];
      },
    },
    dispatchEvent: () => true,
    addEventListener: () => {},
    removeEventListener: () => {},
  });
}

describe("advertising consent", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("defaults to unset and blocks ads without a window", () => {
    expect(readAdvertisingConsent()).toBe("unset");
    expect(hasAdvertisingConsent()).toBe(false);
    expect(canRequestAds()).toBe(false);
  });

  it("does not treat terms consent as advertising consent", () => {
    stubStorage();
    expect(hasAdvertisingConsent()).toBe(false);
    // Nothing has been granted, so ads stay off even though the terms gate may be accepted.
    expect(canRequestAds()).toBe(false);
  });

  it("requires an explicit grant before ads may load", () => {
    stubStorage();
    expect(canRequestAds()).toBe(false);
    setAdvertisingConsent("granted");
    expect(hasAdvertisingConsent()).toBe(true);
    expect(canRequestAds()).toBe(true);
    setAdvertisingConsent("denied");
    expect(hasAdvertisingConsent()).toBe(false);
    expect(canRequestAds()).toBe(false);
  });
});

describe("ad slots", () => {
  it("resolves every placement key to a configured unit", () => {
    for (const key of AD_PLACEMENT_KEYS) {
      expect(resolveAdSlotId(key)).toBe("6603089832");
    }
  });

  it("falls back to the display unit for unknown keys", () => {
    expect(resolveAdSlotId()).toBe("6603089832");
    expect(resolveAdSlotId("not-a-placement")).toBe("6603089832");
  });
});

describe("parseStandingsPayload", () => {
  it("wraps a legacy array as ok/empty", () => {
    expect(parseStandingsPayload([]).status).toBe("empty");
    expect(parseStandingsPayload([{ teamCode: "ARS" }]).status).toBe("ok");
  });

  it("keeps error status from the envelope", () => {
    const parsed = parseStandingsPayload({
      status: "error",
      error: "provider down",
      rows: [],
    });
    expect(parsed.status).toBe("error");
    expect(parsed.error).toBe("provider down");
  });
});

describe("datasetStatusFromMatchweek", () => {
  it("uses query error over payload", () => {
    expect(
      datasetStatusFromMatchweek({ number: 3, matches: [] }, true)
    ).toBe("error");
  });

  it("returns stale from payload", () => {
    expect(
      datasetStatusFromMatchweek(
        { number: 2, matches: [], status: "stale" },
        false
      )
    ).toBe("stale");
  });

  it("infers stale from overdue unfinished fixtures when status is omitted", () => {
    expect(
      datasetStatusFromMatchweek(
        {
          number: 2,
          matches: [
            {
              id: "pl26-mw2-1",
              teamA: "Crystal Palace",
              teamB: "Manchester City",
              kickoffTime: "2026-08-28T19:00:00+00:00",
              status: "NS",
            },
          ],
        },
        false
      )
    ).toBe("stale");
  });

  it("treats future unfinished fixtures as ok when status is omitted", () => {
    expect(
      datasetStatusFromMatchweek(
        {
          number: 3,
          matches: [
            {
              id: "apifb-1",
              teamA: "Liverpool",
              teamB: "Arsenal",
              kickoffTime: "2099-09-12T11:30:00+00:00",
              status: "NS",
            },
          ],
        },
        false
      )
    ).toBe("ok");
  });
});

describe("isUnofficialMatchweek", () => {
  it("infers unofficial from pl26- ids when official is omitted", () => {
    expect(
      isUnofficialMatchweek({
        number: 2,
        matches: [
          {
            id: "pl26-mw2-1",
            teamA: "A",
            teamB: "B",
            kickoffTime: "2026-08-28T19:00:00+00:00",
          },
        ],
      })
    ).toBe(true);
  });

  it("trusts official true even for mock-looking ids", () => {
    expect(
      isUnofficialMatchweek({
        number: 2,
        official: true,
        matches: [
          {
            id: "pl26-mw2-1",
            teamA: "A",
            teamB: "B",
            kickoffTime: "2026-08-28T19:00:00+00:00",
          },
        ],
      })
    ).toBe(false);
  });
});
