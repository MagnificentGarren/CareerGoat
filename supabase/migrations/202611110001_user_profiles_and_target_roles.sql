CREATE TABLE public.profiles (
    id uuid PRIMARY KEY REFERENCES auth.users (id) ON DELETE CASCADE,
    email text NOT NULL,
    display_name varchar(120),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE public.target_roles (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES auth.users (id) ON DELETE CASCADE,
    title varchar(160) NOT NULL CHECK (length(trim(title)) > 0),
    company varchar(160),
    description text NOT NULL CHECK (length(trim(description)) > 0 AND length(description) <= 12000),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX target_roles_user_updated_idx
    ON public.target_roles (user_id, updated_at DESC);

ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.target_roles ENABLE ROW LEVEL SECURITY;

CREATE POLICY profiles_select_own
    ON public.profiles FOR SELECT TO authenticated
    USING (id = (SELECT auth.uid()));

CREATE POLICY profiles_update_own
    ON public.profiles FOR UPDATE TO authenticated
    USING (id = (SELECT auth.uid()))
    WITH CHECK (id = (SELECT auth.uid()));

CREATE POLICY target_roles_select_own
    ON public.target_roles FOR SELECT TO authenticated
    USING (user_id = (SELECT auth.uid()));

CREATE POLICY target_roles_insert_own
    ON public.target_roles FOR INSERT TO authenticated
    WITH CHECK (user_id = (SELECT auth.uid()));

CREATE POLICY target_roles_update_own
    ON public.target_roles FOR UPDATE TO authenticated
    USING (user_id = (SELECT auth.uid()))
    WITH CHECK (user_id = (SELECT auth.uid()));

CREATE POLICY target_roles_delete_own
    ON public.target_roles FOR DELETE TO authenticated
    USING (user_id = (SELECT auth.uid()));

GRANT SELECT ON public.profiles TO authenticated;
GRANT UPDATE (display_name) ON public.profiles TO authenticated;
GRANT SELECT, INSERT, UPDATE, DELETE ON public.target_roles TO authenticated;

CREATE OR REPLACE FUNCTION public.handle_new_auth_user()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_temp
AS $$
BEGIN
    INSERT INTO public.profiles (id, email)
    VALUES (NEW.id, COALESCE(NEW.email, ''))
    ON CONFLICT (id) DO UPDATE SET email = EXCLUDED.email, updated_at = now();
    RETURN NEW;
END;
$$;

CREATE TRIGGER on_auth_user_created
    AFTER INSERT OR UPDATE OF email ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_auth_user();

REVOKE ALL ON FUNCTION public.handle_new_auth_user() FROM PUBLIC, anon, authenticated;

INSERT INTO public.profiles (id, email)
SELECT id, COALESCE(email, '')
FROM auth.users
ON CONFLICT (id) DO UPDATE SET email = EXCLUDED.email;
