from __future__ import annotations
import math
import numpy as np
import gymnasium as gym
from gymnasium import spaces

class NavCubeEnv(gym.Env):
    """2D top-down nav with cube obstacles, mirroring Unity raycast obs."""

    metadata = {"render_modes": []}

    def __init__(self,
                 arena_size: float = 20.0,
                 max_speed: float = 3.5,
                 max_turn: float = math.pi,           # rad/s, ~180 deg/s
                 dt: float = 0.1,
                 max_steps: int = 500,
                 num_obstacles: int = 6,              # ignored if randomize=True
                 obstacle_size: float = 1.5,          # ignored if randomize=True
                 agent_radius: float = 0.5,
                 target_radius: float = 0.7,
                 ray_max_dist: float = 10.0,
                 num_rays: int = 8,
                 seed: int | None = None,
                 randomize: bool = True,              # NEW: random env per episode
                 obstacles_min: int = 3,
                 obstacles_max: int = 12,
                 arena_min: float = 15.0,
                 arena_max: float = 25.0,
                 obstacle_size_min: float = 1.0,
                 obstacle_size_max: float = 2.5):
        super().__init__()
        self.arena_size = arena_size
        self.max_speed = max_speed
        self.max_turn = max_turn
        self.dt = dt
        self.max_steps = max_steps
        self.num_obstacles = num_obstacles
        self.obstacle_size = obstacle_size
        self.agent_radius = agent_radius
        self.target_radius = target_radius
        self.ray_max_dist = ray_max_dist
        self.num_rays = num_rays
        self.diag = arena_size * math.sqrt(2.0)
        # Randomization config — observation normalization uses arena_max
        # so obs stays bounded across episodes
        self.randomize = randomize
        self.obstacles_min = obstacles_min
        self.obstacles_max = obstacles_max
        self.arena_min = arena_min
        self.arena_max = arena_max
        self.obstacle_size_min = obstacle_size_min
        self.obstacle_size_max = obstacle_size_max
        # Use arena_max for normalization so obs stays in [0, ~1] across episodes
        self.diag = arena_max * math.sqrt(2.0) if randomize else arena_size * math.sqrt(2.0)

        self.action_space = spaces.Box(low=-1.0, high=1.0, shape=(2,), dtype=np.float32)
        obs_dim = 2 * num_rays + 5  # rays(dist+type) + vel(2) + dir(2) + dist(1)
        self.observation_space = spaces.Box(low=-1.0, high=1.0, shape=(obs_dim,), dtype=np.float32)

        self._rng = np.random.default_rng(seed)
        self._step = 0
        self._prev_dist = 0.0
        self.agent_pos = np.zeros(2, dtype=np.float32)
        self.agent_heading = 0.0
        self.agent_vel = np.zeros(2, dtype=np.float32)
        self.target_pos = np.zeros(2, dtype=np.float32)
        # obstacles: list of (cx, cy, half) tuples
        self.obstacles: list[tuple[float, float, float]] = []

    # Sampling helpers
    def _sample_free_point(self, min_clearance: float) -> np.ndarray:
        """Sample point not overlapping any obstacle (with margin)."""
        for _ in range(60):
            p = self._rng.uniform(2.0, self.arena_size - 2.0, size=2).astype(np.float32)
            if self._clearance(p) >= min_clearance:
                return p
        return p  # fall back

    def _clearance(self, p: np.ndarray) -> float:
        """Distance from p to closest obstacle surface; +inf if no obstacles."""
        if not self.obstacles:
            return float("inf")
        d = float("inf")
        for cx, cy, h in self.obstacles:
            dx = max(abs(p[0] - cx) - h, 0.0)
            dy = max(abs(p[1] - cy) - h, 0.0)
            d = min(d, math.hypot(dx, dy))
        return d

    # Gym API
    def reset(self, *, seed: int | None = None, options=None):
        if seed is not None:
            self._rng = np.random.default_rng(seed)
        self._step = 0

        # Per-episode randomization — resamples arena_size, num_obstacles,
        # obstacle_size so the policy generalizes across map shapes.
        if self.randomize:
            self.arena_size = float(self._rng.uniform(self.arena_min, self.arena_max))
            target_obstacles = int(self._rng.integers(self.obstacles_min, self.obstacles_max + 1))
            self.obstacle_size = float(self._rng.uniform(self.obstacle_size_min, self.obstacle_size_max))
        else:
            target_obstacles = self.num_obstacles

        # Place obstacles — random cubes, no overlap with each other (best effort)
        self.obstacles = []
        attempts = 0
        while len(self.obstacles) < target_obstacles and attempts < 200:
            attempts += 1
            half = self.obstacle_size / 2.0
            cx, cy = self._rng.uniform(half + 1.0, self.arena_size - half - 1.0, size=2)
            ok = True
            for ex, ey, eh in self.obstacles:
                if abs(cx - ex) < (half + eh + 0.5) and abs(cy - ey) < (half + eh + 0.5):
                    ok = False
                    break
            if ok:
                self.obstacles.append((float(cx), float(cy), half))

        # Place agent and target with clearance
        self.agent_pos = self._sample_free_point(min_clearance=self.agent_radius + 0.3)
        self.agent_heading = float(self._rng.uniform(-math.pi, math.pi))
        self.agent_vel = np.zeros(2, dtype=np.float32)

        # Target far enough from agent
        for _ in range(60):
            t = self._sample_free_point(min_clearance=self.target_radius + 0.3)
            if np.linalg.norm(t - self.agent_pos) > self.arena_size * 0.4:
                self.target_pos = t
                break
        else:
            self.target_pos = t

        self._prev_dist = float(np.linalg.norm(self.target_pos - self.agent_pos))
        return self._obs(), {}

    def step(self, action):
        self._step += 1
        a = np.clip(action, -1.0, 1.0).astype(np.float32)
        thrust = float(a[0])
        turn = float(a[1])

        # Update heading + velocity (kinematic)
        self.agent_heading += turn * self.max_turn * self.dt
        self.agent_heading = (self.agent_heading + math.pi) % (2 * math.pi) - math.pi
        speed = max(0.0, thrust) * self.max_speed
        # also allow small reverse to be useful
        if thrust < 0:
            speed = thrust * self.max_speed * 0.5
        forward = np.array([math.cos(self.agent_heading), math.sin(self.agent_heading)], dtype=np.float32)
        self.agent_vel = forward * speed
        new_pos = self.agent_pos + self.agent_vel * self.dt

        # Compute reward components
        reward = -0.0005  # tiny tick cost so agent doesn't loiter
        terminated = False
        truncated = False

        # Out of arena -> fail
        if (new_pos[0] < 0 or new_pos[0] > self.arena_size or
                new_pos[1] < 0 or new_pos[1] > self.arena_size):
            reward += -1.0
            terminated = True
            self.agent_pos = np.clip(new_pos, 0.0, self.arena_size).astype(np.float32)
            return self._obs(), reward, terminated, truncated, {"reason": "out_of_bounds"}

        # Obstacle collision check (circle-vs-AABB)
        for cx, cy, h in self.obstacles:
            dx = max(abs(new_pos[0] - cx) - h, 0.0)
            dy = max(abs(new_pos[1] - cy) - h, 0.0)
            if math.hypot(dx, dy) < self.agent_radius:
                reward += -0.1
                # don't end episode — agent slides
                # project agent out
                ox, oy = new_pos[0] - cx, new_pos[1] - cy
                if abs(ox) > abs(oy):
                    new_pos[0] = cx + math.copysign(h + self.agent_radius + 0.01, ox)
                else:
                    new_pos[1] = cy + math.copysign(h + self.agent_radius + 0.01, oy)
                break

        self.agent_pos = new_pos.astype(np.float32)
        dist = float(np.linalg.norm(self.target_pos - self.agent_pos))

        # Reaching target -> big reward, end episode
        if dist < (self.agent_radius + self.target_radius):
            reward += 1.0
            terminated = True
            return self._obs(), reward, terminated, truncated, {"reason": "reached"}

        # Progress reward: positive if closer than last step
        progress = self._prev_dist - dist  # >0 if got closer
        reward += 0.5 * progress  # ~0.05 max per step at full speed
        self._prev_dist = dist

        # Timeout
        if self._step >= self.max_steps:
            reward += -0.5
            truncated = True
            return self._obs(), reward, terminated, truncated, {"reason": "timeout"}

        return self._obs(), reward, terminated, truncated, {}

    # Observation construction (mirrors what Unity raycast would give)
    def _obs(self) -> np.ndarray:
        # Rays in agent frame, evenly spaced 360°
        ray_dists = np.zeros(self.num_rays, dtype=np.float32)
        ray_target = np.zeros(self.num_rays, dtype=np.float32)
        for i in range(self.num_rays):
            theta = self.agent_heading + (i / self.num_rays) * 2.0 * math.pi
            d, hit_target = self._cast_ray(self.agent_pos, theta)
            ray_dists[i] = d / self.ray_max_dist
            ray_target[i] = 1.0 if hit_target else 0.0

        # Velocity in agent frame
        forward = np.array([math.cos(self.agent_heading), math.sin(self.agent_heading)], dtype=np.float32)
        right = np.array([math.cos(self.agent_heading - math.pi / 2),
                          math.sin(self.agent_heading - math.pi / 2)], dtype=np.float32)
        v_fwd = float(np.dot(self.agent_vel, forward)) / self.max_speed
        v_lat = float(np.dot(self.agent_vel, right)) / self.max_speed

        # Direction-to-target in agent frame
        delta = self.target_pos - self.agent_pos
        dist = float(np.linalg.norm(delta))
        if dist > 1e-6:
            dir_world = delta / dist
            dir_fwd = float(np.dot(dir_world, forward))
            dir_lat = float(np.dot(dir_world, right))
        else:
            dir_fwd = 1.0
            dir_lat = 0.0

        return np.concatenate([
            ray_dists,
            ray_target,
            np.array([v_fwd, v_lat, dir_fwd, dir_lat, dist / self.diag], dtype=np.float32),
        ]).astype(np.float32)

    def _cast_ray(self, origin: np.ndarray, theta: float) -> tuple[float, bool]:
        """Cast ray from origin in direction theta. Return (distance, hit_was_target).

        Stops at first obstacle/target hit or ray_max_dist.
        """
        dx = math.cos(theta)
        dy = math.sin(theta)
        # Test target sphere
        t_target = self._ray_circle(origin, dx, dy, self.target_pos, self.target_radius)
        # Test each obstacle box
        t_obs = self.ray_max_dist
        for cx, cy, h in self.obstacles:
            t = self._ray_box(origin, dx, dy, cx, cy, h)
            if t < t_obs:
                t_obs = t
        # Choose closer hit
        if t_target < t_obs:
            return min(t_target, self.ray_max_dist), True
        return min(t_obs, self.ray_max_dist), False

    @staticmethod
    def _ray_circle(o: np.ndarray, dx: float, dy: float, c: np.ndarray, r: float) -> float:
        ox, oy = o[0] - c[0], o[1] - c[1]
        b = ox * dx + oy * dy
        cc = ox * ox + oy * oy - r * r
        disc = b * b - cc
        if disc < 0:
            return float("inf")
        sq = math.sqrt(disc)
        t = -b - sq
        if t < 0:
            t = -b + sq
        return t if t > 0 else float("inf")

    @staticmethod
    def _ray_box(o: np.ndarray, dx: float, dy: float, cx: float, cy: float, h: float) -> float:
        # Slab method against axis-aligned box centered at (cx,cy) with half-extent h
        t_min = -float("inf")
        t_max = float("inf")
        for axis, oi, di, ci in ((0, o[0], dx, cx), (1, o[1], dy, cy)):
            if abs(di) < 1e-9:
                if oi < ci - h or oi > ci + h:
                    return float("inf")
                continue
            t1 = (ci - h - oi) / di
            t2 = (ci + h - oi) / di
            if t1 > t2:
                t1, t2 = t2, t1
            t_min = max(t_min, t1)
            t_max = min(t_max, t2)
            if t_min > t_max:
                return float("inf")
        return t_min if t_min > 0 else float("inf")

if __name__ == "__main__":
    env = NavCubeEnv(seed=0)
    obs, _ = env.reset()
    print("obs dim :", obs.shape)
    print("act dim :", env.action_space.shape)
    print("sample obs:", obs[:8].round(3), "...")
    total = 0.0
    for _ in range(200):
        a = env.action_space.sample()
        obs, r, term, trunc, info = env.step(a)
        total += r
        if term or trunc:
            print("ep end :", info, "return:", round(total, 3))
            obs, _ = env.reset()
            total = 0.0
