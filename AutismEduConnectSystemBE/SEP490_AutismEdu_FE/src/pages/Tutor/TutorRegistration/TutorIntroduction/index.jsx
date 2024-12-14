import RemoveIcon from '@mui/icons-material/Remove';
import { Box, Button, FormHelperText, List, ListItem, ListSubheader, Stack, TextField, Typography } from '@mui/material';
import { useFormik } from 'formik';
import PropTypes from 'prop-types';
import * as React from 'react';
import { useEffect, useState } from 'react';
import { NumericFormat } from 'react-number-format';
import ReactQuill, { Quill } from 'react-quill';
import Curriculum from './CurriculumAddition';
import CurriculumDetail from './CurriculumDetail';
import '~/assets/css/ql-editor.css'
const NumericFormatCustom = React.forwardRef(
    function NumericFormatCustom(props, ref) {
        const { onChange, ...other } = props;

        return (
            <NumericFormat
                {...other}
                getInputRef={ref}
                onValueChange={(values) => {
                    onChange({
                        target: {
                            name: props.name,
                            value: values.value,
                        },
                    });
                }}
                isAllowed={(values) => {
                    const { floatValue } = values;
                    return floatValue === undefined || floatValue >= 0;
                }}
                thousandSeparator="."
                decimalSeparator=","
                valueIsNumericString
            />
        );
    },
);

NumericFormatCustom.propTypes = {
    name: PropTypes.string.isRequired,
    onChange: PropTypes.func.isRequired,
};
function TutorIntroduction({ activeStep, handleBack, handleNext, steps, tutorIntroduction, setTutorIntroduction }) {
    const [curriculum, setCurriculum] = useState([]);
    const [value, setValue] = useState({
        fromPrice: "",
        endPrice: ""
    });
    const [contentLength, setContentLength] = useState("");
    const validate = (values) => {
        const errors = {};
        if (!values.startAge || !values.endAge) {
            errors.rangeAge = 'Vui lòng nhập độ tuổi';
        } else if (Number(values.startAge) >= Number(values.endAge)) {
            errors.rangeAge = 'Độ tuổi không hợp lệ';
        }

        if (!values.description) {
            errors.description = "Bắt buộc"
        } else if (contentLength.length > 5000) {
            errors.description = "Giới thiệu quá dài!";
        }
        if (!values.fromPrice) {
            errors.fromPrice = "Bắt buộc"
        }
        else if (Number(values.fromPrice) < 10000) {
            errors.fromPrice = "Số tiền phải lớn hơn 10.000 đồng"
        } else if (Number(values.fromPrice) > 10000000) {
            errors.fromPrice = "Số tiền phải dưới 10.000.000 đồng"
        }
        if (!values.endPrice) {
            errors.endPrice = "Bắt buộc"
        }
        else if (Number(values.endPrice) < 10000) {
            errors.endPrice = "Số tiền phải lớn hơn 10.000 đồng"
        }
        else if (Number(values.endPrice) > 10000000) {
            errors.endPrice = "Số tiền phải dưới 10.000.000 đồng"
        }
        else if (Number(values.endPrice) < Number(values.fromPrice)) {
            errors.endPrice = "Phải lớn hơn số tiền bắt đầu"
        }

        if (!values.sessionHours) {
            errors.sessionHours = "Bắt buộc"
        }
        return errors
    }
    const formik = useFormik({
        initialValues: {
            startAge: '',
            fromPrice: '',
            endPrice: '',
            endAge: '',
            sessionHours: '',
            description: ''
        },
        validate,
        onSubmit: async (values) => {
            let validCurriculum = true;
            curriculum.forEach((c) => {
                if (Number(c.ageFrom) < Number(values.startAge) || Number(c.ageEnd) > Number(values.endAge)) {
                    validCurriculum = false;
                }
            })
            if (validCurriculum) {
                setTutorIntroduction({
                    description: values.description,
                    priceFrom: values.fromPrice,
                    priceEnd: values.endPrice,
                    startAge: values.startAge,
                    endAge: values.endAge,
                    sessionHours: values.sessionHours,
                    curriculum: curriculum
                })
                handleNext();
            }

        }
    });
    useEffect(() => {
        if (tutorIntroduction) {
            formik.setFieldValue("startAge", tutorIntroduction.startAge);
            formik.setFieldValue("endAge", tutorIntroduction.endAge);
            formik.setFieldError("rangeAge", "")
            formik.setFieldValue("description", tutorIntroduction.description)
            formik.setFieldValue("sessionHours", tutorIntroduction.sessionHours)
            setCurriculum(tutorIntroduction.curriculum);
            if (tutorIntroduction.priceFrom && tutorIntroduction.priceEnd) {
                formik.setFieldValue("fromPrice", tutorIntroduction.priceFrom)
                formik.setFieldValue("endPrice", tutorIntroduction.priceEnd)
                setValue({
                    fromPrice: tutorIntroduction.priceFrom ? tutorIntroduction.priceFrom : "",
                    endPrice: tutorIntroduction.priceEnd ? tutorIntroduction.priceEnd : ""
                });
            }
            if (tutorIntroduction.description) {
                const quill = new Quill(document.createElement("div"));
                quill.clipboard.dangerouslyPasteHTML(tutorIntroduction.description);
                const plainText = quill.getText();
                setContentLength(plainText.trim());
            }
        }
    }, [tutorIntroduction])
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
    const handleChangeEdit = (content, delta, source, editor) => {
        const plainText = editor.getText().trim();
        if (plainText === '') {
            formik.setFieldValue("description", "")
            setContentLength("");
        } else {
            formik.setFieldValue("description", content)
            setContentLength(plainText)
        }
    };
    const handleChange = (event) => {
        if (event.target.value[0] === "-") {
            formik.setFieldValue(event.target.name, "0")
        } else {
            formik.setFieldValue(event.target.name, event.target.value)
        }
        setValue({
            ...value,
            [event.target.name]: event.target.value
        });
    };
    return (
        <>
            <Typography variant='h3' textAlign="center" mt={3}>Thông tin gia sư</Typography>
            <form onSubmit={formik.handleSubmit}>
                <Stack direction='row' gap={3}>
                    <Box mt={2} sx={{ width: "60%" }}>
                        <Typography variant='h6' mb={2}>Nhập giới thiệu về bạn</Typography>
                        <ReactQuill
                            value={formik.values.description}
                            name="description"
                            onChange={handleChangeEdit}
                            theme="snow"
                            modules={{
                                toolbar: toolbarOptions
                            }}
                        />
                        <Box textAlign="right" display="flex" sx={{ justifyContent: "space-between" }}>
                            <Box>
                                {
                                    formik.errors.description && (
                                        <FormHelperText error>
                                            {formik.errors.description}
                                        </FormHelperText>
                                    )
                                }
                            </Box>
                            <Typography variant='caption' alignSelf="end">{contentLength.length} / 5000</Typography>
                        </Box>
                        <Typography></Typography>
                    </Box>
                    <Box mt={2} sx={{ width: "60%" }}>
                        <Typography mt={4} mb={2}>Độ tuổi dạy</Typography>
                        <Stack direction='row' alignItems='center' gap={3}>
                            <TextField size='small' label="Từ" type='number' inputProps={{ min: 1, max: 15 }}
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
                            <TextField size='small' label="Đến" type='number' inputProps={{ min: 1, max: 15 }}
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
                            (!formik.values.startAge || !formik.values.endAge || formik.errors.rangeAge) && (
                                <FormHelperText error>
                                    {formik.errors.rangeAge}
                                </FormHelperText>
                            )
                        }
                        <Typography mt={4} mb={2}>Học phí (VNĐ)</Typography>
                        <Stack direction='row' gap={3}>
                            <Box>
                                <TextField
                                    fullWidth
                                    label="Từ"
                                    variant="outlined"
                                    name="fromPrice"
                                    value={value.fromPrice}
                                    onChange={handleChange}
                                    InputProps={{
                                        inputComponent: NumericFormatCustom
                                    }}
                                />
                                {
                                    formik.errors.fromPrice && (
                                        <FormHelperText error>
                                            {formik.errors.fromPrice}
                                        </FormHelperText>
                                    )
                                }
                            </Box>
                            <Box>
                                <TextField
                                    fullWidth
                                    label="Đến"
                                    variant="outlined"
                                    name="endPrice"
                                    value={value.endPrice}
                                    onChange={handleChange}
                                    InputProps={{
                                        inputComponent: NumericFormatCustom
                                    }}
                                />
                                {
                                    formik.errors.endPrice && (
                                        <FormHelperText error>
                                            {formik.errors.endPrice}
                                        </FormHelperText>
                                    )
                                }
                            </Box>
                        </Stack>

                        <Typography mt={4} mb={2}>Số tiếng một buổi học (tiếng)</Typography>
                        <TextField size='small' type='number' inputProps={{ min: 0.5, step: 0.5, max: 10 }}
                            name='sessionHours'
                            value={formik.values.sessionHours}
                            onChange={formik.handleChange}
                        />
                        {
                            formik.errors.sessionHours && (
                                <FormHelperText error>
                                    {formik.errors.sessionHours}
                                </FormHelperText>
                            )
                        }
                        <List
                            sx={{
                                maxWidth: 450, bgcolor: 'background.paper', mt: 3,
                                ".MuiListSubheader-root": {
                                    padding: "0"
                                }
                            }}
                            component="nav"
                            aria-labelledby="nested-list-subheader"
                            disablePadding
                            subheader={
                                <ListSubheader component="div" id="nested-list-subheader">
                                    <Stack direction="row" sx={{ alignItems: "center" }} gap={3}>
                                        <Typography variant='h6'>Thêm khung chương trình</Typography>
                                        {
                                            curriculum.length < 5 && (
                                                <Curriculum curriculum={curriculum} setCurriculum={setCurriculum}
                                                    endAge={formik.values.endAge}
                                                    startAge={formik.values.startAge}
                                                />
                                            )
                                        }
                                    </Stack>
                                    <Typography
                                        variant="caption"
                                        style={{
                                            color: "#ff9800",
                                            fontStyle: "italic",
                                            fontSize: "12px",
                                            marginTop: "8px",
                                            lineHeight: "1.5",
                                            display: "block"
                                        }}
                                    >
                                        Tối đa 5 khung chương trình! Bạn có thể tạo thêm khung chương trình sau khi được hệ thống chấp nhận.
                                    </Typography>
                                </ListSubheader>
                            }
                        >
                            <Box maxHeight="350px" overflow="auto">
                                {
                                    curriculum === null || curriculum.length === 0 ? (
                                        <ListItem>Bạn chưa thêm khung chương trình nào</ListItem>
                                    ) : (
                                        curriculum?.map((c, index) => {
                                            return (
                                                <>
                                                    <CurriculumDetail key={index} index={index} currentCurriculum={c}
                                                        curriculum={curriculum} setCurriculum={setCurriculum}
                                                        startAge={formik.values.startAge}
                                                        endAge={formik.values.endAge}
                                                    />
                                                    {
                                                        (Number(c.ageFrom) < Number(formik.values.startAge) || Number(c.ageEnd) > Number(formik.values.endAge)) && (
                                                            <FormHelperText error sx={{ mb: 2 }}>
                                                                Khung chương trình nằm ngoài độ tuổi dạy
                                                            </FormHelperText>
                                                        )
                                                    }
                                                </>
                                            )
                                        })
                                    )
                                }
                            </Box>
                        </List>
                    </Box>
                </Stack>
                <Box sx={{ display: 'flex', flexDirection: 'row', pt: 2, mt: 3 }}>
                    <Button
                        color="inherit"
                        disabled={activeStep === 0}
                        onClick={() => {
                            handleBack();
                            setTutorIntroduction({
                                description: formik.values.description,
                                priceFrom: formik.values.fromPrice,
                                priceEnd: formik.values.endPrice,
                                startAge: formik.values.startAge,
                                endAge: formik.values.endAge,
                                sessionHours: formik.values.sessionHours,
                                curriculum: curriculum
                            })
                        }}
                        sx={{ mr: 1 }}
                    >
                        Quay lại
                    </Button>
                    <Box sx={{ flex: '1 1 auto' }} />
                    <Button type='submit'>
                        {activeStep === steps.length - 1 ? 'Kết thúc' : 'Tiếp theo'}
                    </Button>
                </Box>
            </form>
        </>
    )
}

export default TutorIntroduction
