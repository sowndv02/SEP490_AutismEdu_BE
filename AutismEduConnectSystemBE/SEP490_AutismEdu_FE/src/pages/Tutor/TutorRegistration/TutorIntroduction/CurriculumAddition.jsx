import AddCircleOutlineIcon from '@mui/icons-material/AddCircleOutline';
import RemoveIcon from '@mui/icons-material/Remove';
import { FormHelperText, Grid, IconButton, Stack, TextField } from '@mui/material';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Modal from '@mui/material/Modal';
import Typography from '@mui/material/Typography';
import { useFormik } from 'formik';
import { enqueueSnackbar } from 'notistack';
import * as React from 'react';
import ReactQuill from 'react-quill';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 850,
    maxHeight: "90vh",
    bgcolor: 'background.paper',
    boxShadow: 24,
    overflowY: "auto",
    p: 4,
};

export default function Curriculum({ curriculum, setCurriculum, endAge, startAge }) {
    const [open, setOpen] = React.useState(false);
    const [editorContent, setEditorContent] = React.useState("");
    const [editorErr, setEditorErr] = React.useState("Vui lòng nhập khung chương trình!");
    const [content, setContent] = React.useState("");
    const handleOpen = () => {
        if (endAge === "" || startAge === "") {
            enqueueSnackbar("Vui lòng nhập độ tuổi dạy", { variant: "warning" })
        } else
            setOpen(true);
    }
    const handleClose = () => {
        formik.resetForm();
        setEditorContent("");
        setOpen(false);
        setEditorErr(true);
        setContent("");
        setEditorErr("Vui lòng nhập khung chương trình!")
    }
    const validate = values => {
        const errors = {};
        if (!values.startAge || !values.endAge) {
            errors.rangeAge = 'Vui lòng nhập độ tuổi';
        } else if (Number(values.startAge) >= Number(values.endAge)) {
            errors.rangeAge = 'Độ tuổi không hợp lệ';
        }
        return errors;
    };

    const toolbarOptions = [
        ['bold', 'italic', 'underline', 'strike'],
        ['blockquote', 'code-block'],
        ['link', 'formula'],
        [{ 'header': 1 }, { 'header': 2 }],
        [{ 'list': 'ordered' }, { 'list': 'bullet' }, { 'list': 'check' }],
        [{ 'script': 'sub' }, { 'script': 'super' }],
        [{ 'indent': '-1' }, { 'indent': '+1' }],
        [{ 'direction': 'rtl' }],
        [{ 'size': ['small', false, 'large', 'huge'] }],
        [{ 'header': [1, 2, 3, 4, 5, 6, false] }],
        [{ 'color': [] }, { 'background': [] }],
        [{ 'font': [] }],
        [{ 'align': [] }],
        ['clean']
    ];
    const formik = useFormik({
        initialValues: {
            startAge: "",
            endAge: ""
        },
        validate,
        onSubmit: (values) => {
            let existCur = false;
            curriculum.forEach((f) => {
                if (f.ageFrom === values.startAge && f.ageEnd === values.endAge) {
                    existCur = true;
                }
            })
            if (existCur) {
                enqueueSnackbar("Khung chương trình này đã tồn tại!", { variant: "error" })
            }
            if (!editorErr && !existCur) {
                setCurriculum(pre => [...pre, {
                    ageFrom: values.startAge,
                    ageEnd: values.endAge,
                    description: editorContent,
                    content: content
                }])
                handleClose();
                formik.resetForm();
                setEditorContent("");
            }
        }
    });

    const handleChangeEdit = (content, delta, source, editor) => {
        const plainText = editor.getText().trim();
        setEditorContent(plainText)
        if (plainText === "") {
            setEditorContent("")
            setEditorErr("Vui lòng nhập khung chương trình!");
            setContent("");
        }
        else {
            if (plainText.length > 2000) setEditorErr("Nội dung quá dài!");
            else setEditorErr("");
            setEditorContent(content);
            setContent(plainText);
        }
    };
    return (
        <div>
            <IconButton onClick={handleOpen}><AddCircleOutlineIcon /></IconButton>
            <Modal
                open={open}
                onClose={handleClose}
            >
                <Box sx={style}>
                    <Typography id="modal-modal-title" variant="h5" component="h2">
                        Thêm khung chương trình
                    </Typography>
                    <form onSubmit={formik.handleSubmit}>
                        <Grid container px="50px" py="50px" columnSpacing={2} rowSpacing={3}>
                            <Grid item xs={2} textAlign="right">Chọn độ tuổi:</Grid>
                            <Grid item xs={10}>
                                <Stack direction='row' alignItems='center' gap={3}>
                                    <TextField size='small' label="Từ" type='number' inputProps={{ min: startAge, max: endAge }}
                                        name='startAge'
                                        value={formik.values.startAge}
                                        onChange={(e) => {
                                            const value = e.target.value;
                                            if (Number.isInteger(Number(value)) || value === '') {
                                                formik.setFieldValue('startAge', value);
                                            }
                                        }}
                                    />
                                    <RemoveIcon />
                                    <TextField size='small' label="Đến" type='number' inputProps={{ min: startAge, max: endAge }}
                                        name='endAge'
                                        value={formik.values.endAge}
                                        onChange={(e) => {
                                            const value = e.target.value;
                                            if (Number.isInteger(Number(value)) || value === '') {
                                                formik.setFieldValue('endAge', value);
                                            }
                                        }} />
                                </Stack>
                                {
                                    formik.errors.rangeAge && (
                                        <FormHelperText error>
                                            {formik.errors.rangeAge}
                                        </FormHelperText>
                                    )
                                }
                            </Grid>
                            <Grid item xs={2} textAlign="right">Nội dung: </Grid>
                            <Grid item xs={10}>
                                <Box mt={2} sx={{ height: "400px", width: "100%" }}>
                                    <ReactQuill
                                        value={editorContent}
                                        onChange={handleChangeEdit}
                                        theme="snow"
                                        modules={{
                                            toolbar: toolbarOptions
                                        }}
                                        style={{ height: "300px" }}
                                    />
                                </Box>
                                <Box textAlign="right" display="flex" sx={{ justifyContent: "space-between" }}>
                                    <Box>
                                        {
                                            editorErr && (
                                                <FormHelperText error>
                                                    {editorErr}
                                                </FormHelperText>
                                            )
                                        }
                                    </Box>
                                    <Typography variant='caption' alignSelf="end">{content.length} / 2000</Typography>
                                </Box>

                            </Grid>

                        </Grid>
                        <Box sx={{ display: "flex", justifyContent: "end", gap: 2 }}>
                            <Button variant='contained' type='submit'>Thêm</Button>
                            <Button onClick={handleClose}>Huỷ</Button>
                        </Box>
                    </form>

                </Box>
            </Modal>
        </div>
    );
}