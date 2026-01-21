use camino::Utf8Path;
use std::{env, path::PathBuf, process::Command};

fn main() {
    println!("cargo:rerun-if-changed=build.rs");
    let out_dir = "bindings";
    let udl_file = "src/snapxrust.udl";
    let cargo = Utf8Path::new("Cargo.toml");
    uniffi_build::generate_scaffolding(udl_file).unwrap();
    // if Command::new("$HOME/.cargo/bin/uniffi-bindgen-cs").arg("--version").output().is_err() {
    // Using fork to bump uniffi to 0.30
    println!("Installing uniffi-bindgen-cs updated fork!");
    Command::new("cargo")
        .arg("install")
        .arg("uniffi-bindgen-cs")
        .arg("--git")
        .arg("https://github.com/sensslen/uniffi-bindgen-cs")
        .arg("--branch")
        .arg("main")
        .status()
        .expect("Failed to install uniffi-bindgen-cs");
    // }
    let home = env::var("HOME")
        .or_else(|_| env::var("USERPROFILE"))
        .expect("Could not find HOME or USERPROFILE environment variables");

    let mut bindgen_path = PathBuf::from(home).join(".cargo/bin/uniffi-bindgen-cs");

    if cfg!(target_os = "windows") {
        bindgen_path.set_extension("exe");
    }

    let output = Command::new(bindgen_path)
        .arg(udl_file)
        .arg("--config")
        .arg(cargo)
        .arg("--out_dir")
        .arg(out_dir)
        .output();
}
