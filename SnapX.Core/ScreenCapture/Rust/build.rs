use camino::Utf8Path;
use std::{
    env,
    path::PathBuf,
    process::{Command, Stdio},
};

fn main() {
    println!("cargo:rerun-if-changed=build.rs");
    let out_dir = "bindings";
    let udl_file = "src/snapxrust.udl";
    let cargo = Utf8Path::new("Cargo.toml");
    uniffi_build::generate_scaffolding(udl_file).unwrap();
    let home = env::var("HOME")
        .or_else(|_| env::var("USERPROFILE"))
        .expect("Could not find HOME or USERPROFILE environment variables");
    let mut bindgen_path: PathBuf = PathBuf::from(home).join(".cargo/bin/uniffi-bindgen-cs");
    if cfg!(target_os = "windows") {
        bindgen_path.set_extension("exe");
    }
    let required_version = "uniffi-bindgen 0.10.0+v0.30.0";
    let mut needs_install = true;
    if let Ok(output) = Command::new(&bindgen_path).arg("--version").output() {
        if output.status.success() {
            let version_str = String::from_utf8_lossy(&output.stdout);

            if version_str.trim() == required_version {
                println!("cargo:warning=Correct version found: {}", required_version);
                needs_install = false;
            } else {
                println!(
                    "cargo:warning=Version mismatch! Found: '{}', Expected: '{}'",
                    version_str.trim(),
                    required_version
                );
            }
        }
    }
    if needs_install {
        println!("cargo:warning=uniffi-bindgen-cs not found or invalid. Installing updated fork!");
        let status = Command::new("cargo")
            .arg("install")
            .arg("uniffi-bindgen-cs")
            .arg("--git")
            .arg("https://github.com/sensslen/uniffi-bindgen-cs")
            .arg("--branch")
            .arg("main")
            .stdout(Stdio::inherit())
            .stderr(Stdio::inherit())
            .status()
            .expect("Failed to install uniffi-bindgen-cs");
        if !status.success() {
            panic!("Failed to install uniffi-bindgen-cs via cargo");
        }
    }

    let status = Command::new(&bindgen_path)
        .arg(udl_file)
        .arg("--config")
        .arg(cargo)
        .arg("--out-dir")
        .arg(out_dir)
        .stdin(Stdio::null())
        .stdout(Stdio::inherit())
        .stderr(Stdio::inherit())
        .status()
        .expect("Failed to run uniffi-bindgen");

    if !status.success() {
        panic!("uniffi-bindgen failed with status: {}", status);
    }
}
